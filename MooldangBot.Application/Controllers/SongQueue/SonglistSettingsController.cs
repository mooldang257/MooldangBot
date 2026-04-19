using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.SongQueue
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class SonglistSettingsController(
        IAppDbContext db,
        IOverlayNotificationService notificationService) : ControllerBase
    {
        [HttpGet("/api/settings/data/{streamerId}")]
        public async Task<IActionResult> GetSettings(string streamerId)
        {
            var profile = await GetProfileByUidOrSlugAsync(streamerId);
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            var settings = new SonglistSettingsResponseDto
            {
                DesignSettingsJson = profile.DesignSettingsJson ?? "{}",
                SongRequestCommands = await db.UnifiedCommands
                    .AsNoTracking()
                    .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
                    .Select(c => new SongRequestCommandDto
                    {
                        Name = c.Icon, // [물멍]: SongRequestCommandDto의 Name 필드에 아이콘(또는 명칭) 매핑
                        Keyword = c.Keyword,
                        Price = c.Cost
                    })
                    .ToListAsync(),
                Omakases = await db.StreamerOmakases
                    .AsNoTracking()
                    .Where(o => o.StreamerProfileId == profile.Id && !db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && c.IsDeleted))
                    .Join(db.UnifiedCommands.Where(c => c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted),
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

        [HttpPost("/api/settings/update/{streamerId}")]
        public async Task<IActionResult> UpdateSettings(string streamerId, [FromBody] SonglistSettingsUpdateRequest request)
        {
            var profile = await GetProfileByUidOrSlugAsync(streamerId);
            if (profile == null)
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

            // 1. 디자인 설정 업데이트
            profile.DesignSettingsJson = request.DesignSettingsJson;

            // 2. 노래 신청용 명령어 동기화 (간소화된 Sync 로직)
            var existingSongCommands = await db.UnifiedCommands
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
                .ToListAsync();

            // 기존 것 모두 삭제 및 신규 추가 (가장 확실한 동기화 방식)
            foreach (var cmd in existingSongCommands) cmd.IsDeleted = true;

            foreach (var cmdDto in request.SongRequestCommands)
            {
                db.UnifiedCommands.Add(new UnifiedCommand
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
            var existingOmakases = await db.StreamerOmakases
                .Where(o => o.StreamerProfileId == profile.Id)
                .ToListAsync();
            
            var existingCommands = await db.UnifiedCommands
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
                    db.StreamerOmakases.Add(newOmakase);
                    await db.SaveChangesAsync(); // ID 확보를 위해 일단 저장

                    db.UnifiedCommands.Add(new UnifiedCommand
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
            
            // [물멍]: 설정 변경 알림 (필요 시 오버레이 등에 전파)
            await notificationService.NotifySongQueueChangedAsync(profile.ChzzkUid);

            return Ok(Result<bool>.Success(true));
        }

        private async Task<StreamerProfile?> GetProfileByUidOrSlugAsync(string uid)
        {
            var target = uid.ToLower();
            return await db.StreamerProfiles
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
        }
    }
}
