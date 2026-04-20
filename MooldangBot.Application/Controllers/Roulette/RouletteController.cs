using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MediatR;
using MooldangBot.Modules.Roulette.Features.Commands.SpinRoulette;
using MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;

namespace MooldangBot.Application.Controllers.Roulette
{
    [ApiController]
    [ApiController]
    [Route("api/admin/roulette")]
    [Authorize(Policy = "ChannelManager")]
    public class RouletteController(IAppDbContext db, IMediator mediator) : ControllerBase
    {
        private string? GetChzzkUid()
        {
            return User.FindFirst("StreamerId")?.Value;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetRoulettes(string chzzkUid, [FromQuery] PagedRequest request)
        {
            var query = db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.StreamerProfile)
                .Where(R => R.StreamerProfile!.ChzzkUid == chzzkUid);

            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(R => R.Id < request.Cursor.Value);
            }

            var pagedResult = await query
                .Join(db.UnifiedCommands.IgnoreQueryFilters(),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.FeatureType == CommandFeatureType.Roulette)
                .OrderByDescending(x => x.Roulette.Id)
                .Select(x => new RouletteSummaryDto
                {
                    Id = x.Roulette.Id,
                    Name = x.Roulette.Name,
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive,
                    ActiveItemCount = x.Roulette.Items.Count(I => I.IsActive),
                    LstUpdDt = x.Roulette.UpdatedAt
                })
                .AsNoTracking()
                .ToPagedListAsync(request.Limit, r => r.Id);

            return Ok(Result<PagedResponse<RouletteSummaryDto>>.Success(pagedResult));
        }

        [HttpGet("{chzzkUid}/{Id}")]
        public async Task<IActionResult> GetRoulette(string chzzkUid, int Id)
        {
            var consolidated = await db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.Items)
                .Include(R => R.StreamerProfile)
                .Where(r => r.Id == Id && r.StreamerProfile!.ChzzkUid == chzzkUid)
                .Join(db.UnifiedCommands.IgnoreQueryFilters(),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.FeatureType == CommandFeatureType.Roulette)
                .Select(x => new RouletteResponseDto
                {
                    Id = x.Roulette.Id,
                    Name = x.Roulette.Name,
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive,
                    UpdatedAt = x.Roulette.UpdatedAt,
                    Items = x.Roulette.Items.Select(i => new RouletteItemResponseDto
                    {
                        Id = i.Id,
                        ItemName = i.ItemName,
                        Probability = i.Probability,
                        Probability10x = i.Probability10x,
                        Color = i.Color,
                        IsMission = i.IsMission,
                        Template = i.Template,
                        IsActive = i.IsActive,
                        SoundUrl = i.SoundUrl,
                        UseDefaultSound = i.UseDefaultSound
                    }).ToList()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (consolidated == null) 
                return NotFound(Result<string>.Failure("룰렛을 찾을 수 없습니다."));

            return Ok(Result<RouletteResponseDto>.Success(consolidated));
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> CreateRoulette(string chzzkUid, [FromBody] RouletteUpdateRequest req)
        {
            var streamer = await db.StreamerProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            if (!req.Items.Any() || req.Items.Sum(I => I.Probability) <= 0)
            {
                return BadRequest(Result<string>.Failure("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다."));
            }

            // 1. 룰렛 엔티티 생성 및 저장
            var RouletteObj = new Domain.Entities.Roulette
            {
                StreamerProfileId = streamer.Id,
                Name = req.Name,
                UpdatedAt = KstClock.Now,
                Items = req.Items.Select(i => new Domain.Entities.RouletteItem
                {
                    ItemName = i.ItemName,
                    Probability = i.Probability,
                    Probability10x = i.Probability10x,
                    Color = i.Color,
                    IsMission = i.IsMission,
                    Template = i.Template,
                    IsActive = i.IsActive,
                    SoundUrl = i.SoundUrl,
                    UseDefaultSound = i.UseDefaultSound
                }).ToList()
            };

            db.Roulettes.Add(RouletteObj);
            await db.SaveChangesAsync();

            // 2. 연결된 통합 명령어(UnifiedCommand) 생성
            var UnifiedCmd = new UnifiedCommand
            {
                StreamerProfileId = streamer.Id,
                Keyword = req.Command ?? "!룰렛",
                FeatureType = CommandFeatureType.Roulette,
                TargetId = RouletteObj.Id,
                Cost = req.CostPerSpin,
                CostType = req.Type == RouletteType.Cheese ? CommandCostType.Cheese : CommandCostType.Point,
                ResponseText = req.Name,
                RequiredRole = CommandRole.Viewer,
                MatchType = CommandMatchType.Prefix,
                RequiresSpace = true,
                IsActive = req.IsActive,
                CreatedAt = KstClock.Now,
                UpdatedAt = KstClock.Now
            };

            db.UnifiedCommands.Add(UnifiedCmd);
            await db.SaveChangesAsync();

            return Ok(Result<RouletteResponseDto>.Success(new RouletteResponseDto
            {
                Id = RouletteObj.Id,
                Name = RouletteObj.Name,
                Type = req.Type,
                Command = UnifiedCmd.Keyword,
                CostPerSpin = UnifiedCmd.Cost,
                IsActive = UnifiedCmd.IsActive,
                UpdatedAt = RouletteObj.UpdatedAt,
                Items = RouletteObj.Items.Select(i => new RouletteItemResponseDto
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    Probability = i.Probability,
                    Probability10x = i.Probability10x,
                    Color = i.Color,
                    IsMission = i.IsMission,
                    Template = i.Template,
                    IsActive = i.IsActive,
                    SoundUrl = i.SoundUrl,
                    UseDefaultSound = i.UseDefaultSound
                }).ToList()
            }));
        }

        [HttpPut("{chzzkUid}/{Id}")]
        public async Task<IActionResult> UpdateRoulette(string chzzkUid, int Id, [FromBody] RouletteUpdateRequest req)
        {
            var RouletteObj = await db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.Items)
                .Include(R => R.StreamerProfile)
                .FirstOrDefaultAsync(R => R.Id == Id && R.StreamerProfile!.ChzzkUid == chzzkUid);

            if (RouletteObj == null) 
                return NotFound(Result<string>.Failure("룰렛을 찾을 수 없습니다."));

            RouletteObj.Name = req.Name;
            RouletteObj.UpdatedAt = KstClock.Now;

            db.RouletteItems.RemoveRange(RouletteObj.Items);
            RouletteObj.Items = req.Items.Select(i => new Domain.Entities.RouletteItem
            {
                RouletteId = Id,
                ItemName = i.ItemName,
                Probability = i.Probability,
                Probability10x = i.Probability10x,
                Color = i.Color,
                IsMission = i.IsMission,
                Template = i.Template,
                IsActive = i.IsActive,
                SoundUrl = i.SoundUrl,
                UseDefaultSound = i.UseDefaultSound
            }).ToList();

            var UnifiedCmd = await db.UnifiedCommands
                .IgnoreQueryFilters()
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.TargetId == Id 
                                        && c.StreamerProfile!.ChzzkUid == chzzkUid 
                                        && c.FeatureType == CommandFeatureType.Roulette);

            if (UnifiedCmd != null)
            {
                UnifiedCmd.Keyword = req.Command ?? UnifiedCmd.Keyword;
                UnifiedCmd.Cost = req.CostPerSpin;
                UnifiedCmd.CostType = req.Type == RouletteType.Cheese ? CommandCostType.Cheese : CommandCostType.Point;
                UnifiedCmd.IsActive = req.IsActive;
                UnifiedCmd.UpdatedAt = KstClock.Now;
            }

            await db.SaveChangesAsync();

            return Ok(Result<RouletteResponseDto>.Success(new RouletteResponseDto
            {
                Id = RouletteObj.Id,
                Name = RouletteObj.Name,
                Type = req.Type,
                Command = UnifiedCmd?.Keyword ?? string.Empty,
                CostPerSpin = UnifiedCmd?.Cost ?? 0,
                IsActive = UnifiedCmd?.IsActive ?? false,
                UpdatedAt = RouletteObj.UpdatedAt,
                Items = RouletteObj.Items.Select(i => new RouletteItemResponseDto
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    Probability = i.Probability,
                    Probability10x = i.Probability10x,
                    Color = i.Color,
                    IsMission = i.IsMission,
                    Template = i.Template,
                    IsActive = i.IsActive,
                    SoundUrl = i.SoundUrl,
                    UseDefaultSound = i.UseDefaultSound
                }).ToList()
            }));
        }

        [HttpPatch("{chzzkUid}/{Id}/status")]
        public async Task<IActionResult> ToggleRouletteStatus(string chzzkUid, int Id, [FromBody] bool isActiveParam)
        {
            var streamer = await db.StreamerProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);

            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var AffectedRows = await db.UnifiedCommands.IgnoreQueryFilters()
                    .Where(C => C.TargetId == Id 
                             && C.StreamerProfileId == streamer.Id 
                             && C.FeatureType == CommandFeatureType.Roulette)
                    .ExecuteUpdateAsync(S => S.SetProperty(C => C.IsActive, isActiveParam));

            return AffectedRows == 0 
                ? NotFound(Result<string>.Failure("룰렛 명령어를 찾을 수 없습니다.")) 
                : Ok(Result<bool>.Success(true));
        }

        [HttpPost("complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteAnimation([FromBody] CompleteRequest Request, CancellationToken ct)
        {
            var success = await mediator.Send(new CompleteRouletteCommand(Request.SpinId), ct);
            
            if (success)
            {
                return Ok(Result<object>.Success(new { Success = true }));
            }

            return BadRequest(Result<string>.Failure("이미 처리되었거나 유효하지 않은 SpinId입니다."));
        }

        [HttpPatch("{chzzkUid}/items/{ItemId}/status")]
        public async Task<IActionResult> ToggleItemStatus(string chzzkUid, int ItemId, [FromBody] bool isActiveParam)
        {
            var AffectedRows = await db.RouletteItems.IgnoreQueryFilters()
                    .Where(I => I.Id == ItemId && I.Roulette != null && I.Roulette.StreamerProfile!.ChzzkUid == chzzkUid)
                    .ExecuteUpdateAsync(S => S.SetProperty(I => I.IsActive, isActiveParam));

            if (AffectedRows > 0)
            {
                await db.Roulettes.IgnoreQueryFilters()
                        .Where(R => R.Items.Any(I => I.Id == ItemId))
                        .ExecuteUpdateAsync(S => S.SetProperty(R => R.UpdatedAt, KstClock.Now));
                    
                return Ok(Result<bool>.Success(true));
            }

            return NotFound(Result<string>.Failure("룰렛 아이템을 찾을 수 없습니다."));
        }

        [HttpDelete("{chzzkUid}/{Id}")]
        public async Task<IActionResult> DeleteRoulette(string chzzkUid, int Id)
        {
            var RouletteObj = await db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.StreamerProfile)
                .FirstOrDefaultAsync(R => R.Id == Id && R.StreamerProfile!.ChzzkUid == chzzkUid);

            if (RouletteObj == null) 
                return NotFound(Result<string>.Failure("룰렛을 찾을 수 없습니다."));

            db.Roulettes.Remove(RouletteObj);
            await db.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "룰렛이 삭제되었습니다." }));
        }

        [HttpGet("{chzzkUid}/history")]
        public async Task<IActionResult> GetHistory(
            string chzzkUid, 
            [FromQuery] RouletteLogStatus? status = null, 
            [FromQuery] string? nickname = null,
            [FromQuery] int? rouletteId = null,
            [FromQuery] string? itemName = null,
            [FromQuery] PagedRequest request = null!)
        {
            var query = db.RouletteLogs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(l => l.StreamerProfile)
                .Include(l => l.GlobalViewer) 
                .Where(l => l.StreamerProfile!.ChzzkUid == chzzkUid);

            // 필터링 적용
            if (status.HasValue) query = query.Where(l => l.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(nickname)) query = query.Where(l => l.GlobalViewer!.Nickname.Contains(nickname));
            if (rouletteId.HasValue && rouletteId.Value > 0) query = query.Where(l => l.RouletteId == rouletteId.Value);
            if (!string.IsNullOrWhiteSpace(itemName)) query = query.Where(l => l.ItemName.Contains(itemName));
            
            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(l => l.Id < request.Cursor.Value);
            }

            var pagedResult = await query
                .OrderByDescending(l => l.Id)
                .Select(l => new RouletteLogDto(
                    l.Id, 
                    l.RouletteId, 
                    l.RouletteName, 
                    l.GlobalViewer!.Nickname, 
                    l.ItemName, 
                    l.CreatedAt, 
                    (int)l.Status
                ))
                .ToPagedListAsync(request.Limit, l => l.Id);

            return Ok(Result<PagedResponse<RouletteLogDto>>.Success(pagedResult));
        }

        [HttpPut("{chzzkUid}/history/{id}/status")]
        public async Task<IActionResult> UpdateStatus(string chzzkUid, long id, [FromBody] RouletteLogStatus status)
        {
            var log = await db.RouletteLogs
                .IgnoreQueryFilters()
                .Include(l => l.StreamerProfile)
                .FirstOrDefaultAsync(l => l.Id == id && l.StreamerProfile!.ChzzkUid == chzzkUid);

            if (log == null) 
                return NotFound(Result<string>.Failure("로그를 찾을 수 없거나 접근 권한이 없습니다."));

            log.Status = status;
            log.ProcessedAt = KstClock.Now;
            await db.SaveChangesAsync();

            return Ok(Result<RouletteLog>.Success(log));
        }

        [HttpDelete("{chzzkUid}/history/{id}")]
        public async Task<IActionResult> DeleteHistory(string chzzkUid, long id)
        {
            var log = await db.RouletteLogs
                .IgnoreQueryFilters()
                .Include(l => l.StreamerProfile)
                .FirstOrDefaultAsync(l => l.Id == id && l.StreamerProfile!.ChzzkUid == chzzkUid);

            if (log == null) 
                return NotFound(Result<string>.Failure("삭제할 로그를 찾을 수 없거나 접근 권한이 없습니다."));

            db.RouletteLogs.Remove(log);
            await db.SaveChangesAsync();

            return Ok(Result<bool>.Success(true));
        }

        [HttpDelete("{chzzkUid}/history/bulk")]
        public async Task<IActionResult> BulkDeleteHistory(string chzzkUid, [FromBody] List<long> ids)
        {
            var affectedRows = await db.RouletteLogs
                .IgnoreQueryFilters()
                .Where(l => ids.Contains(l.Id) && l.StreamerProfile!.ChzzkUid == chzzkUid)
                .ExecuteDeleteAsync();

            return Ok(Result<int>.Success(affectedRows));
        }

        [HttpPost("{chzzkUid}/{Id}/test")]
        public async Task<IActionResult> TestSpin(string chzzkUid, int Id, [FromQuery] bool Is10x = false)
        {
            int count = Is10x ? 10 : 1;
            var result = await mediator.Send(new SpinRouletteCommand(chzzkUid, Id, "admin_test", count, "관리자"));
            
            if (result == null || !result.Items.Any()) 
                return NotFound(Result<string>.Failure("룰렛을 찾을 수 없거나 활성화된 아이템이 없습니다."));
                
            return Ok(Result<object>.Success(result.Items));
        }
    }
}
