using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MooldangBot.Application.Hubs;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Controllers.Overlay;

/// <summary>
/// [오버레이 프리셋 관제소]: 오버레이 디자인 프리셋의 저장, 조회, 적용 및 관련 자산 업로드를 담당합니다.
/// </summary>
[ApiController]
[Route("api/v1/overlay/presets")]
[Authorize]
public class OverlayPresetController(
    IAppDbContext db,
    IUserSession userSession,
    IOverlayNotificationService notificationService,
    IIdentityCacheService identityCache,
    IWebHostEnvironment env,
    IHubContext<OverlayHub> hubContext) : ControllerBase
{
    /// <summary>
    /// [프리셋 목록 조회]: 사용 가능한 프리셋 목록을 가져옵니다. (기본 프리셋 자동 생성 포함)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<Result<List<OverlayPresetDto>>>> GetPresets()
    {
        var currentUid = userSession.ChzzkUid;
        if (string.IsNullOrEmpty(currentUid)) return Unauthorized();

        // 스트리머 프로필 확인
        var profile = await db.TableCoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == currentUid);
            
        if (profile == null) return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

        // 내 프리셋 + 공개된 공식 프리셋 조회 (프로필 조인하여 UID 확보)
        var presets = await db.TableSysOverlayPresets
            .AsNoTracking()
            .Include(p => p.CoreStreamerProfiles)
            .Where(p => p.IsPublic || p.StreamerProfileId == profile.Id)
            .Select(p => new OverlayPresetDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ConfigJson = p.ConfigJson,
                IsPublic = p.IsPublic,
                CreatorChzzkUid = p.CoreStreamerProfiles != null ? p.CoreStreamerProfiles.ChzzkUid : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        // [물멍]: 내 프리셋이 하나도 없는 경우 기본 프리셋 하나를 생성해줍니다.
        if (!presets.Any(p => p.CreatorChzzkUid == currentUid))
        {
            var defaultPreset = new SysOverlayPresets
            {
                StreamerProfileId = profile.Id,
                Name = "기본 레이아웃",
                Description = "물댕봇의 표준 오버레이 레이아웃입니다.",
                ConfigJson = profile.DesignSettingsJson ?? "{}",
                IsPublic = false,
                CreatedAt = KstClock.Now,
                UpdatedAt = KstClock.Now
            };
            db.TableSysOverlayPresets.Add(defaultPreset);
            await db.SaveChangesAsync();
            
            presets.Add(new OverlayPresetDto
            {
                Id = defaultPreset.Id,
                Name = defaultPreset.Name,
                Description = defaultPreset.Description,
                ConfigJson = defaultPreset.ConfigJson,
                IsPublic = defaultPreset.IsPublic,
                CreatorChzzkUid = currentUid,
                CreatedAt = defaultPreset.CreatedAt,
                UpdatedAt = defaultPreset.UpdatedAt
            });
        }

        var result = presets
            .OrderByDescending(p => p.IsPublic)
            .ThenByDescending(p => p.UpdatedAt)
            .ToList();

        return Ok(Result<object>.Success(new {
            Presets = result,
            ActivePresetId = profile.ActiveOverlayPresetId
        }));
    }

    /// <summary>
    /// [프리셋 생성]: 현재 설정을 프리셋으로 저장합니다.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Result<int>>> CreatePreset([FromBody] CreateOverlayPresetRequest request)
    {
        var currentUid = userSession.ChzzkUid;
        if (string.IsNullOrEmpty(currentUid)) return Unauthorized();

        var profile = await db.TableCoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == currentUid);
            
        if (profile == null) return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

        var preset = new SysOverlayPresets
        {
            StreamerProfileId = profile.Id,
            Name = request.Name,
            Description = request.Description,
            ConfigJson = request.ConfigJson,
            IsPublic = request.IsPublic && userSession.Role == "master",
            CreatedAt = KstClock.Now,
            UpdatedAt = KstClock.Now
        };

        db.TableSysOverlayPresets.Add(preset);
        
        // [물멍]: 프리셋 생성 시 해당 프리셋을 현재 적용된 상태로 간주
        profile.ActiveOverlayPresetId = preset.Id;
        
        await db.SaveChangesAsync();

        return Ok(Result<int>.Success(preset.Id));
    }

    /// <summary>
    /// [프리셋 수정]: 프리셋 정보를 업데이트합니다.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<bool>>> UpdatePreset(int id, [FromBody] UpdateOverlayPresetRequest request)
    {
        var currentUid = userSession.ChzzkUid;
        var preset = await db.TableSysOverlayPresets
            .Include(p => p.CoreStreamerProfiles)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preset == null) return NotFound(Result<bool>.Failure("프리셋을 찾을 수 없습니다."));
        
        // 소유권 확인: 프로필의 UID와 현재 유저의 UID 비교
        if (preset.CoreStreamerProfiles?.ChzzkUid != currentUid && userSession.Role != "master") 
            return Forbid();

        preset.Name = request.Name;
        preset.Description = request.Description;
        preset.IsPublic = request.IsPublic && userSession.Role == "master";
        preset.UpdatedAt = KstClock.Now;

        await db.SaveChangesAsync();
        return Ok(Result<bool>.Success(true));
    }

    /// <summary>
    /// [프리셋 삭제]
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<bool>>> DeletePreset(int id)
    {
        var currentUid = userSession.ChzzkUid;
        var preset = await db.TableSysOverlayPresets
            .Include(p => p.CoreStreamerProfiles)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preset == null) return NotFound(Result<bool>.Failure("프리셋을 찾을 수 없습니다."));
        
        if (preset.CoreStreamerProfiles?.ChzzkUid != currentUid && userSession.Role != "master") 
            return Forbid();

        db.TableSysOverlayPresets.Remove(preset);
        await db.SaveChangesAsync();
        
        return Ok(Result<bool>.Success(true));
    }

    /// <summary>
    /// [프리셋 적용]: 선택한 프리셋을 현재 오버레이에 즉시 반영합니다.
    /// </summary>
    [HttpPost("{id}/apply")]
    public async Task<ActionResult<Result<bool>>> ApplyPreset(int id)
    {
        var currentUid = userSession.ChzzkUid;
        if (string.IsNullOrEmpty(currentUid)) return Unauthorized();

        var preset = await db.TableSysOverlayPresets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (preset == null) return NotFound(Result<bool>.Failure("프리셋을 찾을 수 없습니다."));

        var profile = await db.TableCoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == currentUid);
        if (profile == null) return NotFound(Result<bool>.Failure("스트리머 정보를 찾을 수 없습니다."));

        // 1. DB 업데이트
        profile.DesignSettingsJson = preset.ConfigJson;
        profile.ActiveOverlayPresetId = id;
        await db.SaveChangesAsync();

        // 2. 캐시 무효화
        identityCache.InvalidateStreamer(currentUid);
        if (!string.IsNullOrEmpty(profile.Slug)) identityCache.InvalidateSlug(profile.Slug);

        // 3. 실시간 오버레이 갱신 알림
        await notificationService.BroadcastSongOverlayUpdateAsync(currentUid);
        await hubContext.Clients.Group(currentUid.ToLower()).SendAsync("ReceiveOverlayStyle", preset.ConfigJson);

        return Ok(Result<bool>.Success(true));
    }

    /// <summary>
    /// [현재 설정 즉시 저장]: 프리셋과 별개로 현재 에디터의 설정을 스트리머 프로필에 즉시 저장합니다.
    /// </summary>
    [HttpPost("save-current")]
    public async Task<ActionResult<Result<bool>>> SaveCurrentConfig([FromBody] SaveOverlaySettingsRequest request)
    {
        var currentUid = userSession.ChzzkUid;
        if (string.IsNullOrEmpty(currentUid)) return Unauthorized();

        var profile = await db.TableCoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == currentUid);
        if (profile == null) return NotFound(Result<bool>.Failure("스트리머 정보를 찾을 수 없습니다."));

        var configJson = request.Config.GetRawText();

        // 1. DB 업데이트
        profile.DesignSettingsJson = configJson;
        profile.ActiveOverlayPresetId = request.PresetId; // [물멍]: 미리보기 중인 프리셋 ID를 저장
        
        await db.SaveChangesAsync();

        // 2. 캐시 무효화 및 알림
        identityCache.InvalidateStreamer(currentUid);
        if (!string.IsNullOrEmpty(profile.Slug)) identityCache.InvalidateSlug(profile.Slug);
        
        await notificationService.BroadcastSongOverlayUpdateAsync(currentUid);
        await hubContext.Clients.Group(currentUid.ToLower()).SendAsync("ReceiveOverlayStyle", configJson);

        return Ok(Result<bool>.Success(true));
    }

    public class SaveOverlaySettingsRequest
    {
        public JsonElement Config { get; set; }
        public int? PresetId { get; set; }
    }

    /// <summary>
    /// [이미지 업로드]: 오버레이용 배경 이미지 등을 업로드합니다.
    /// </summary>
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        var currentUid = userSession.ChzzkUid;
        if (string.IsNullOrEmpty(currentUid)) return Unauthorized();

        if (image == null || image.Length == 0)
            return BadRequest(Result<string>.Failure("업로드할 파일이 없거나 비어있습니다."));

        try 
        {
            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".webm" };
            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExts.Contains(ext))
                return BadRequest(Result<string>.Failure("허용되지 않는 파일 형식입니다."));

            string uploadsFolder = Path.Combine(env.WebRootPath, "images", "overlays");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{currentUid}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            string fileUrl = $"/images/overlays/{fileName}";
            return Ok(Result<object>.Success(new { url = fileUrl }));
        }
        catch (Exception ex)
        {
            return BadRequest(Result<string>.Failure($"이미지 업로드 중 오류가 발생했습니다: {ex.Message}"));
        }
    }
}
