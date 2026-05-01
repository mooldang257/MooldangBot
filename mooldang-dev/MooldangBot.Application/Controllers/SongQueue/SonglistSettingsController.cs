using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.SongQueue
{
    [ApiController]
    [Route("api/config/songlist/{chzzkUid}")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 활용
    public class SonglistSettingsController(
        IAppDbContext db,
        IOverlayNotificationService notificationService,
        MooldangBot.Domain.Abstractions.IAuthService authService,
        IIdentityCacheService identityCache) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetSettings(string chzzkUid)
        {
            var profile = await GetCachedProfileAsync(chzzkUid);
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // [물멍]: 오버레이 주소 생성을 위해 토큰 보강 (없으면 자동 생성)
            var overlayToken = await authService.IssueOverlayTokenAsync(profile.ChzzkUid, "Streamer");

            var settings = new SonglistSettingsResponseDto
            {
                OverlayToken = overlayToken,
                DesignSettingsJson = profile.DesignSettingsJson ?? "{}",
                SongRequestCommands = await db.SysUnifiedCommands
                    .AsNoTracking()
                    .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
                    .Select(c => new SongRequestCommandDto
                    {
                        Name = c.Icon, // [물멍]: SongRequestCommandDto의 Name 필드에 아이콘(또는 명칭) 매핑
                        Keyword = c.Keyword,
                        Price = c.Cost
                    })
                    .ToListAsync(),
                Omakases = await db.FuncStreamerOmakases
                    .AsNoTracking()
                    .Where(o => o.StreamerProfileId == profile.Id && !db.SysUnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && c.IsDeleted))
                    .Join(db.SysUnifiedCommands.Where(c => c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted),
                        o => o.Id,
                        c => c.TargetId,
                        (o, c) => new OmakaseDto
                        {
                            Id = o.Id,
                            Name = c.ResponseText, // [물멍]: 오마카세 명칭은 ResponseText에 저장됨
                            Icon = o.Icon,
                            Price = c.Cost,
                            Count = o.Count,
                            Command = c.Keyword
                        })
                    .ToListAsync()
            };

            return Ok(Result<SonglistSettingsResponseDto>.Success(settings));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string chzzkUid, [FromBody] SonglistSettingsUpdateRequest request)
        {
            // [오시리스의 영속]: 업데이트를 위해 DB에서 직접 조회하여 트래킹 상태로 만듭니다.
            var profile = await db.CoreStreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == chzzkUid.ToLower());
            
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // 1. 디자인 설정 업데이트
            profile.DesignSettingsJson = request.DesignSettingsJson;

            // 2. 노래 신청용 명령어 동기화 (간소화된 Sync 로직)
            var existingSongCommands = await db.SysUnifiedCommands
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
                .ToListAsync();

            // 기존 것 모두 삭제 및 신규 추가 (가장 확실한 동기화 방식)
            foreach (var cmd in existingSongCommands) cmd.IsDeleted = true;

            foreach (var cmdDto in request.SongRequestCommands)
            {
                db.SysUnifiedCommands.Add(new UnifiedCommand
                {
                    StreamerProfileId = profile.Id,
                    FeatureType = CommandFeatureType.SongRequest,
                    Keyword = cmdDto.Keyword,
                    Icon = cmdDto.Name,
                    Cost = cmdDto.Price,
                    CostType = CommandCostType.Cheese,
                    ResponseText = "노래 신청이 접수되었습니다.",
                    MatchType = CommandMatchType.Prefix,
                    IsActive = true
                });
            }

            // 3. 오마카세 메뉴 동기화
            var incomingOmakaseIds = request.Omakases.Select(o => o.Id).ToList();
            var existingOmakases = await db.FuncStreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id)
                .ToListAsync();
            
            var existingCommands = await db.SysUnifiedCommands
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted)
                .ToListAsync();

            foreach (var oDto in request.Omakases)
            {
                if (oDto.Id > 0 && oDto.Id < 2000000000) // 기존 수정
                {
                    var oEntity = existingOmakases.FirstOrDefault(o => o.Id == oDto.Id);
                    if (oEntity != null)
                    {
                        oEntity.Icon = oDto.Icon;
                        oEntity.Count = oDto.Count;
                        
                        var cEntity = existingCommands.FirstOrDefault(c => c.TargetId == oEntity.Id);
                        if (cEntity != null)
                        {
                            cEntity.Keyword = oDto.Command;
                            cEntity.ResponseText = oDto.Name;
                            cEntity.Cost = oDto.Price;
                        }
                    }
                }
                else // 신규 추가
                {
                    var newOmakase = new StreamerOmakaseItem
                    {
                        StreamerProfileId = profile.Id,
                        Icon = oDto.Icon,
                        Count = oDto.Count,
                        IsActive = true
                    };
                    db.FuncStreamerOmakases.Add(newOmakase);
                    await db.SaveChangesAsync(); // ID 확보를 위해 일단 저장

                    db.SysUnifiedCommands.Add(new UnifiedCommand
                    {
                        StreamerProfileId = profile.Id,
                        FeatureType = CommandFeatureType.Omakase,
                        Keyword = oDto.Command,
                        Icon = oDto.Icon,
                        Cost = oDto.Price,
                        CostType = CommandCostType.Cheese,
                        ResponseText = oDto.Name,
                        TargetId = newOmakase.Id,
                        MatchType = CommandMatchType.Exact,
                        IsActive = true
                    });
                }
            }

            await db.SaveChangesAsync();
            
            // [이지스의 눈]: DB가 업데이트되었으므로 캐시를 무효화하여 다음 조회 시 최신 정보를 읽도록 합니다.
            identityCache.InvalidateStreamer(profile.ChzzkUid);
            if (!string.IsNullOrEmpty(profile.Slug))
            {
                identityCache.InvalidateSlug(profile.Slug);
            }
            
            // [물멍]: 설정 변경 알림 및 오버레이 전체 동기화 (디자인 변경점 즉시 반영)
            await notificationService.BroadcastSongOverlayUpdateAsync(profile.ChzzkUid);

            return Ok(Result<bool>.Success(true));
        }

        private async Task<StreamerProfile?> GetCachedProfileAsync(string uid)
        {
            var profile = await identityCache.GetStreamerProfileAsync(uid);
            if (profile != null) return profile;

            var target = uid.ToLower();
            return await db.CoreStreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
        }
    }
}
