using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
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
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/roulette")]
    [Route("api/admin/roulette")]
    [Authorize(Policy = "ChannelManager")]
    public class RouletteController(IAppDbContext db, IMediator mediator) : ControllerBase
    {
        private string? GetChzzkUid()
        {
            return User.FindFirst("StreamerId")?.Value;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetRoulettes(string chzzkUid, [FromQuery] int LastId = 0, [FromQuery] int PageSize = 10)
        {
            var RawData = await db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.StreamerProfile)
                .Where(R => R.StreamerProfile!.ChzzkUid == chzzkUid && (LastId == 0 || R.Id < LastId))
                .Join(db.UnifiedCommands.IgnoreQueryFilters(),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.FeatureType == CommandFeatureType.Roulette)
                .OrderByDescending(x => x.Roulette.Id)
                .Take(PageSize + 1)
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
                .ToListAsync();

            var HasNext = RawData.Count > PageSize;
            var OutputData = HasNext ? RawData[..PageSize] : RawData;
            int? NextLastId = HasNext ? OutputData[^1].Id : null;

            return Ok(Result<PagedResponse<RouletteSummaryDto>>.Success(new PagedResponse<RouletteSummaryDto>(Data: OutputData, NextLastId: NextLastId)));
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
                .Select(x => new 
                {
                    Id = x.Roulette.Id,
                    ChzzkUid = x.Roulette.StreamerProfile!.ChzzkUid,
                    Name = x.Roulette.Name,
                    UpdatedAt = x.Roulette.UpdatedAt,
                    Items = x.Roulette.Items,
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (consolidated == null) 
                return NotFound(Result<string>.Failure("룰렛을 찾을 수 없습니다."));

            foreach (var I in consolidated.Items) I.Roulette = null;
            return Ok(Result<object>.Success(consolidated));
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
                Items = req.Items
            };

            foreach (var I in RouletteObj.Items) 
            {
                I.Id = 0;
                I.Roulette = null;
            }

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

            foreach (var I in RouletteObj.Items) I.Roulette = null;
            return Ok(Result<Domain.Entities.Roulette>.Success(RouletteObj));
        }

        [HttpPost("{chzzkUid}/{Id}")]
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
            foreach (var Item in req.Items)
            {
                Item.Id = 0;
                Item.RouletteId = Id;
            }
            RouletteObj.Items = req.Items;

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
            foreach (var I in RouletteObj.Items) I.Roulette = null;

            return Ok(Result<Domain.Entities.Roulette>.Success(RouletteObj));
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
        public async Task<IActionResult> GetHistory(string chzzkUid, [FromQuery] RouletteLogStatus? status = null, [FromQuery] long lastId = 0, [FromQuery] int pageSize = 20)
        {
            var query = db.RouletteLogs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(l => l.StreamerProfile)
                .Include(l => l.GlobalViewer) 
                .Where(l => l.StreamerProfile!.ChzzkUid == chzzkUid);

            if (status.HasValue) query = query.Where(l => l.Status == status.Value);
            if (lastId > 0) query = query.Where(l => l.Id < lastId);

            var logs = await query
                .OrderByDescending(l => l.Id)
                .Take(pageSize + 1)
                .Select(l => new RouletteLogDto(
                    l.Id, 
                    l.RouletteId, 
                    l.RouletteName, 
                    l.GlobalViewer!.Nickname, 
                    l.ItemName, 
                    l.CreatedAt, 
                    (int)l.Status
                ))
                .ToListAsync();

            var hasNext = logs.Count > pageSize;
            var outputData = hasNext ? logs[..pageSize] : logs;
            long? nextLastId = hasNext ? outputData[^1].Id : null;

            return Ok(Result<PagedResponse<RouletteLogDto>>.Success(new PagedResponse<RouletteLogDto>(Data: outputData, NextLastId: (int?)nextLastId)));
        }

        [HttpPut("history/{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] RouletteLogStatus status)
        {
            var streamerUid = User.FindFirst("StreamerId")?.Value ?? "None";
            var log = await db.RouletteLogs
                .IgnoreQueryFilters()
                .Include(l => l.StreamerProfile)
                .FirstOrDefaultAsync(l => l.Id == id && l.StreamerProfile!.ChzzkUid == streamerUid);

            if (log == null) 
                return NotFound(Result<string>.Failure("로그를 찾을 수 없거나 접근 권한이 없습니다."));

            log.Status = status;
            log.ProcessedAt = KstClock.Now;
            await db.SaveChangesAsync();

            return Ok(Result<RouletteLog>.Success(log));
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
