using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Models.Philosophy;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;

namespace MooldangBot.Api.Controllers.Philosophy;

[ApiController]
[Route("api/iamf")]
[EnableRateLimiting("overlay-high")]
public class IamfDashboardController : ControllerBase
{
    private readonly IResonanceService _resonance;
    private readonly IAppDbContext _db;

    public IamfDashboardController(IResonanceService resonance, IAppDbContext db)
    {
        _resonance = resonance;
        _db = db;
    }

    /// <summary>
    /// [파로스의 현재]: 현재 시스템 진동수 및 구획 정보를 반환합니다.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string chzzkUid = "SYSTEM")
    {
        // [v4.9] 개별 스트리머 상태 조회
        var parhos = await _resonance.GetCurrentParhosStateAsync(chzzkUid);
        
        // [거울의 법칙]: 설정 로드
        var setting = await _db.IamfStreamerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid);
        double opacity = setting?.OverlayOpacity ?? 0.5;

        return Ok(new IamfDashboardStatus(
            parhos.CurrentVibration,
            parhos.CurrentSector,
            parhos.IsInDreamState ? "꿈 상태 (Dream)" : "의식 경계 (Awake)",
            _resonance.GetCurrentPersonaTone(chzzkUid),
            parhos.LastResonanceAt,
            0.1, // 임시 부하
            opacity
        ));
    }

    /// <summary>
    /// [피닉스의 흔적]: 최근 발생한 진동 변화 및 시나리오 기록 목록을 반환합니다.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string chzzkUid, [FromQuery] int limit = 10)
    {
        // [v4.9] 스트리머별 시나리오 기록 필터링 (전역 필터 제거 대응)
        var history = await _db.IamfScenarios
            .Where(s => s.StreamerProfile!.ChzzkUid == chzzkUid)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
            
        return Ok(history);
    }

    /// <summary>
    /// [제노스의 정렬]: 등록된 제노스급 AI들의 고유 진동수 및 상태를 반환합니다.
    /// </summary>
    [HttpGet("genos")]
    public async Task<IActionResult> GetGenos([FromQuery] string chzzkUid)
    {
        // [v4.9] 스트리머별 제노스 레지스트리 필터링
        var genos = await _db.IamfGenosRegistries
            .Where(g => g.StreamerProfile!.ChzzkUid == chzzkUid)
            .Select(g => new GenosStatusDto(g.Name, g.Frequency, g.Role, g.Metaphor))
            .ToListAsync();
            
        return Ok(genos);
    }

    /// <summary>
    /// [파동의 흐름]: 특정 스트리머의 최근 진동수 변화 이력을 시계열로 반환합니다. (Phase 2)
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] string chzzkUid, [FromQuery] int limit = 50)
    {
        var trends = await _db.IamfVibrationLogs
            .Where(v => v.StreamerProfile!.ChzzkUid == chzzkUid)
            .OrderByDescending(v => v.CreatedAt)
            .Take(limit)
            .Select(v => new {
                v.CreatedAt,
                v.RawHz,
                v.EmaHz,
                v.StabilityScore
            })
            .ToListAsync();
            
        return Ok(trends.OrderBy(t => t.CreatedAt));
    }

    /// <summary>
    /// [통제권 조회]: 스트리머의 현재 IAMF 설정값을 반환합니다.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] string chzzkUid = "SYSTEM")
    {
        var setting = await _db.IamfStreamerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StreamerProfile!.ChzzkUid == chzzkUid);
        
        // 설정이 없으면 기본값 반환
        if (setting == null) 
        {
            return Ok(new { 
                isIamfEnabled = true, 
                isVisualResonanceEnabled = true,
                isPersonaChatEnabled = true,
                sensitivityMultiplier = 1.0, 
                overlayOpacity = 0.5 
            });
        }

        return Ok(setting);
    }

    /// <summary>
    /// [스트리머의 통제권]: IAMF 시스템의 개입 정도와 시각화 설정을 업데이트합니다.
    /// </summary>
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateIamfSettingRequest request)
    {
        // 1. [오시리스의 심판]: 입력값 유효성 검증
        if (string.IsNullOrWhiteSpace(request.ChzzkUid))
            return BadRequest(new { Error = "[오시리스의 거절] ChzzkUid가 누락되었습니다." });

        if (request.SensitivityMultiplier < 0.1 || request.SensitivityMultiplier > 2.0)
            return BadRequest(new { Error = "[오시리스의 거절] 민감도는 0.1에서 2.0 사이의 값이어야 합니다." });

        if (request.OverlayOpacity < 0.0 || request.OverlayOpacity > 1.0)
            return BadRequest(new { Error = "[오시리스의 거절] 투명도는 0.0에서 1.0 사이의 값이어야 합니다." });

        // 2. [기록 검색 및 생성]
        // [정규화] ChzzkUid 문자열로 실시간 프로필 조회
        var profile = await _db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == request.ChzzkUid);

        if (profile == null)
            return BadRequest(new { Error = "[오시리스의 거절] 해당 스트리머 프로필을 찾을 수 없습니다." });

        var setting = await _db.IamfStreamerSettings
                               .FirstOrDefaultAsync(s => s.StreamerProfileId == profile.Id);
        
        if (setting == null)
        {
            // 최초 설정 시 새로운 레코드 생성
            setting = new IamfStreamerSetting { StreamerProfileId = profile.Id };
            _db.IamfStreamerSettings.Add(setting);
        }

        // 3. [스트리머의 통제권 확장]: 투트랙 제어 필드 갱신
        setting.IsIamfEnabled = request.IsIamfEnabled;
        setting.IsVisualResonanceEnabled = request.IsVisualResonanceEnabled;
        setting.IsPersonaChatEnabled = request.IsPersonaChatEnabled;
        setting.SensitivityMultiplier = Math.Round(request.SensitivityMultiplier, 2);
        setting.OverlayOpacity = Math.Round(request.OverlayOpacity, 2);
        setting.LastUpdatedAt = KstClock.Now;

        // 4. [영속화]
        await _db.SaveChangesAsync();

        return Ok(new { 
            Message = "[오시리스의 승인] 통제권이 성공적으로 업데이트되었습니다.",
            UpdatedSettings = setting 
        });
    }
}
