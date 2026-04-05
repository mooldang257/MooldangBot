using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Features.Roulette;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangBot.Presentation.Features.Roulette
{
    // [v6.2.6] 이지스의 정화: 로컬 DTO를 Domain.DTOs로 통합 이주 완료
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/roulette")] // 새로운 버전 명시 경로
    [Route("api/admin/roulette")]                       // 레거시 하위 호환 경로
    [Authorize(Policy = "ChannelManager")]
    public class RouletteController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly IRouletteService _rouletteService;
        private readonly IMemoryCache _cache;

        public RouletteController(IAppDbContext db, IRouletteService rouletteService, IMemoryCache cache)
        {
            _db = db;
            _rouletteService = rouletteService;
            _cache = cache;
        }

        private string? GetChzzkUid()
        {
            return User.FindFirst("StreamerId")?.Value;
        }

        [HttpGet("{chzzkUid}")]
        public async Task<IActionResult> GetRoulettes(string chzzkUid, [FromQuery] int LastId = 0, [FromQuery] int PageSize = 10)
        {
            var RawData = await _db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.StreamerProfile)
                .Where(R => R.StreamerProfile!.ChzzkUid == chzzkUid && (LastId == 0 || R.Id < LastId))
                .Join(_db.UnifiedCommands.IgnoreQueryFilters()
                    .Include(c => c.MasterFeature),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.MasterFeature!.TypeName == "Roulette")
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

            return Ok(new PagedResponse<RouletteSummaryDto>(Data: OutputData, NextLastId: NextLastId));
        }

        [HttpGet("{chzzkUid}/{Id}")]
        public async Task<IActionResult> GetRoulette(string chzzkUid, int Id)
        {
            var consolidated = await _db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.Items)
                .Include(R => R.StreamerProfile)
                .Where(r => r.Id == Id && r.StreamerProfile!.ChzzkUid == chzzkUid)
                .Join(_db.UnifiedCommands.IgnoreQueryFilters()
                    .Include(c => c.MasterFeature),
                    r => r.Id,
                    c => c.TargetId,
                    (r, c) => new { Roulette = r, Command = c })
                .Where(x => x.Command.MasterFeature!.TypeName == "Roulette")
                .Select(x => new 
                {
                    Id = x.Roulette.Id,
                    ChzzkUid = x.Roulette.StreamerProfile!.ChzzkUid,
                    Name = x.Roulette.Name,
                    UpdatedAt = x.Roulette.UpdatedAt,
                    Items = x.Roulette.Items,
                    // UnifiedCommand 정보 병합
                    Type = x.Command.CostType == CommandCostType.Cheese ? RouletteType.Cheese : RouletteType.ChatPoint,
                    Command = x.Command.Keyword,
                    CostPerSpin = x.Command.Cost,
                    IsActive = x.Command.IsActive
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (consolidated == null) return NotFound();

            foreach (var I in consolidated.Items) I.Roulette = null;
            return Ok(consolidated);
        }

        [HttpPost("{chzzkUid}")]
        public async Task<IActionResult> CreateRoulette(string chzzkUid, [FromBody] MooldangBot.Domain.Entities.Roulette RouletteObj)
        {
            try
            {
                RouletteObj.Id = 0;
                
                var streamer = await _db.StreamerProfiles.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);
                if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

                RouletteObj.StreamerProfileId = streamer.Id;
                RouletteObj.UpdatedAt = KstClock.Now;
                
                if (!RouletteObj.Items.Any() || RouletteObj.Items.Sum(I => I.Probability) <= 0)
                {
                    return BadRequest("최소 하나 이상의 아이템과 유효한 확률 정보가 필요합니다.");
                }

                foreach (var I in RouletteObj.Items) I.Roulette = null;

                _db.Roulettes.Add(RouletteObj);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRoulette), new { chzzkUid, Id = RouletteObj.Id }, RouletteObj);
            }
            catch (Exception Ex)
            {
                return StatusCode(500, $"서버 에러(생성): {Ex.Message}");
            }
        }

        [HttpPost("{chzzkUid}/{Id}")]
        public async Task<IActionResult> UpdateRoulette(string chzzkUid, int Id, [FromBody] RouletteUpdateRequest req)
        {
            try
            {
                // [v6.2.5] 이지스의 정화: manual validation 제거 (FluentValidation에 위임)
                var RouletteObj = await _db.Roulettes
                    .IgnoreQueryFilters()
                    .Include(R => R.Items)
                    .Include(R => R.StreamerProfile)
                    .FirstOrDefaultAsync(R => R.Id == Id && R.StreamerProfile!.ChzzkUid == chzzkUid);

                if (RouletteObj == null) return NotFound();

                // 1. 룰렛 기본 정보 업데이트
                RouletteObj.Name = req.Name;
                RouletteObj.UpdatedAt = KstClock.Now;

                _db.RouletteItems.RemoveRange(RouletteObj.Items);
                foreach (var Item in req.Items)
                {
                    Item.Id = 0;
                    Item.RouletteId = Id;
                }
                RouletteObj.Items = req.Items;

                // 2. [추가] UnifiedCommand 정보 역동기화 (v4.3 정문화 반영)
                var UnifiedCmd = await _db.UnifiedCommands
                    .IgnoreQueryFilters()
                    .Include(c => c.StreamerProfile)
                    .Include(c => c.MasterFeature)
                    .FirstOrDefaultAsync(c => c.TargetId == Id 
                                           && c.StreamerProfile!.ChzzkUid == chzzkUid 
                                           && c.MasterFeature!.TypeName == "Roulette");

                if (UnifiedCmd != null)
                {
                    UnifiedCmd.Keyword = req.Command ?? UnifiedCmd.Keyword;
                    UnifiedCmd.Cost = req.CostPerSpin;
                    UnifiedCmd.CostType = req.Type == RouletteType.Cheese ? CommandCostType.Cheese : CommandCostType.Point;
                    UnifiedCmd.IsActive = req.IsActive;
                    UnifiedCmd.UpdatedAt = KstClock.Now;
                }

                await _db.SaveChangesAsync();
                foreach (var I in RouletteObj.Items) I.Roulette = null;

                return Ok(RouletteObj);
            }
            catch (Exception Ex)
            {
                return StatusCode(500, $"서버 에러(수정): {Ex.Message}");
            }
        }

        [HttpPatch("{chzzkUid}/{Id}/status")]
        public async Task<IActionResult> ToggleRouletteStatus(string chzzkUid, int Id, [FromBody] bool isActiveParam)
        {
            // [v4.3] 정문화된 필터링: StreamerProfileId와 MasterFeature를 활용한 벌크 업데이트
            var streamer = await _db.StreamerProfiles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ChzzkUid == chzzkUid);

            if (streamer == null) return NotFound("스트리머를 찾을 수 없습니다.");

            var AffectedRows = await _db.UnifiedCommands.IgnoreQueryFilters()
                    .Where(C => C.TargetId == Id 
                             && C.StreamerProfileId == streamer.Id 
                             && C.MasterFeature!.TypeName == "Roulette")
                    .ExecuteUpdateAsync(S => S.SetProperty(C => C.IsActive, isActiveParam));

            return AffectedRows == 0 ? NotFound() : Ok();
        }

        [HttpPost("complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteAnimation([FromBody] CompleteRequest Request, CancellationToken ct)
        {
            // [v6.2.5] 이지스의 정화: manual validation 제거 (FluentValidation에 위임)

            // [v9.1] 지능형 상호작용: 메모리 캐시 의존성을 제거하고 DB 영속성(SpinId) 기반으로 완료 처리합니다.
            var success = await _rouletteService.CompleteRouletteAsync(Request.SpinId, ct);
            
            if (success)
            {
                return Ok(new { Success = true });
            }

            return NotFound("이미 처리되었거나 유효하지 않은 SpinId입니다.");
        }

        [HttpPatch("{chzzkUid}/items/{ItemId}/status")]
        public async Task<IActionResult> ToggleItemStatus(string chzzkUid, int ItemId, [FromBody] bool isActiveParam)
        {
            var AffectedRows = await _db.RouletteItems.IgnoreQueryFilters()
                    .Where(I => I.Id == ItemId && I.Roulette != null && I.Roulette.StreamerProfile!.ChzzkUid == chzzkUid)
                    .ExecuteUpdateAsync(S => S.SetProperty(I => I.IsActive, isActiveParam));

            if (AffectedRows > 0)
            {
                await _db.Roulettes.IgnoreQueryFilters()
                        .Where(R => R.Items.Any(I => I.Id == ItemId))
                        .ExecuteUpdateAsync(S => S.SetProperty(R => R.UpdatedAt, KstClock.Now));
                    
                return Ok();
            }

            return NotFound();
        }

        [HttpDelete("{chzzkUid}/{Id}")]
        public async Task<IActionResult> DeleteRoulette(string chzzkUid, int Id)
        {
            var RouletteObj = await _db.Roulettes
                .IgnoreQueryFilters()
                .Include(R => R.StreamerProfile)
                .FirstOrDefaultAsync(R => R.Id == Id && R.StreamerProfile!.ChzzkUid == chzzkUid);

            if (RouletteObj == null) return NotFound();

            _db.Roulettes.Remove(RouletteObj);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{chzzkUid}/history")]
        public async Task<IActionResult> GetHistory(string chzzkUid, [FromQuery] RouletteLogStatus? status = null, [FromQuery] long lastId = 0, [FromQuery] int pageSize = 20)
        {
            var query = _db.RouletteLogs
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(l => l.StreamerProfile)
                .Include(l => l.GlobalViewer) // [v6.2] 닉네임 조회를 위해 포함
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
                    l.GlobalViewer!.Nickname, // [v6.2] 정문화된 닉네임 사용
                    l.ItemName, 
                    l.CreatedAt, 
                    (int)l.Status
                ))
                .ToListAsync();

            var hasNext = logs.Count > pageSize;
            var outputData = hasNext ? logs[..pageSize] : logs;
            long? nextLastId = hasNext ? outputData[^1].Id : null;

            return Ok(new PagedResponse<RouletteLogDto>(Data: outputData, NextLastId: (int?)nextLastId));
        }

        [HttpPut("history/{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] RouletteLogStatus status)
        {
            var streamerUid = User.FindFirst("StreamerId")?.Value ?? "None";
            var log = await _db.RouletteLogs
                .IgnoreQueryFilters()
                .Include(l => l.StreamerProfile)
                .FirstOrDefaultAsync(l => l.Id == id && l.StreamerProfile!.ChzzkUid == streamerUid);

            if (log == null) return NotFound("로그를 찾을 수 없거나 접근 권한이 없습니다.");

            log.Status = status;
            log.ProcessedAt = KstClock.Now;
            await _db.SaveChangesAsync();

            return Ok(log);
        }

        [HttpPost("{chzzkUid}/{Id}/test")]
        public async Task<IActionResult> TestSpin(string chzzkUid, int Id, [FromQuery] bool Is10x = false)
        {
            if (Is10x)
            {
                var Results = await _rouletteService.SpinRoulette10xAsync(chzzkUid, Id, "admin_test");
                return Ok(Results);
            }
            else
            {
                var Result = await _rouletteService.SpinRouletteAsync(chzzkUid, Id, "admin_test");
                return Ok(Result);
            }
        }
    }
}
