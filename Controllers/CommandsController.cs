using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;

using MooldangAPI.Services;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 모든 명령어 관리에 채널 매니저 정책 적용
    public class CommandsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICommandCacheService _cacheService;
        private readonly ILogger<CommandsController> _logger;

        public CommandsController(AppDbContext db, ICommandCacheService cacheService, ILogger<CommandsController> logger)
        {
            _db = db;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet("/api/commands/list/{chzzkUid}")]
        public async Task<IResult> GetCommands(string chzzkUid)
        {
            var combinedList = new List<CombinedCommandDto>();
            var targetUid = chzzkUid.Trim().ToLower();
            
            _logger.LogInformation($"🔍 [CommandsApi] 명령어 리스트 요청 수신. TargetUid: {targetUid}");

            // 1. 일반 커스텀 명령어 (StreamerCommands)
            var customCmds = await _db.StreamerCommands
                .IgnoreQueryFilters() // 💡 [마스터 대응] 필터 우회
                .AsNoTracking()
                .Where(c => c.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();

            _logger.LogInformation($"🔍 [CommandsApi] DB 조회 결과: {customCmds.Count}개의 커스텀 명령어 발견.");
                
            combinedList.AddRange(customCmds.Select(c => new CombinedCommandDto
            {
                Id = $"Custom:{c.Id}",
                Keyword = c.CommandKeyword,
                Type = "Custom",
                ActionType = c.ActionType,
                Description = c.Content,
                RequiredRole = c.RequiredRole
            }));

            // 2. 스트리머 프로필 기본 설정 명령어
            var profile = await _db.StreamerProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid);
                
            if (profile != null)
            {
                // 노래 신청
                if (!string.IsNullOrEmpty(profile.SongCommand))
                {
                    combinedList.Add(new CombinedCommandDto
                    {
                        Id = "Profile:Song",
                        Keyword = profile.SongCommand,
                        Type = "Song",
                        ActionType = "SongRequest",
                        Description = "노래 신청 기능 활성화",
                        RequiredRole = "all"
                    });
                }
                // 출석 체크
                if (!string.IsNullOrEmpty(profile.AttendanceCommands))
                {
                    combinedList.Add(new CombinedCommandDto
                    {
                        Id = "Profile:Attendance",
                        Keyword = profile.AttendanceCommands,
                        Type = "Attendance",
                        ActionType = "System",
                        Description = "출석 체크 및 인사말 응답",
                        RequiredRole = "all"
                    });
                }
                // 포인트 확인
                if (!string.IsNullOrEmpty(profile.PointCheckCommand))
                {
                    combinedList.Add(new CombinedCommandDto
                    {
                        Id = "Profile:Point",
                        Keyword = profile.PointCheckCommand,
                        Type = "Point",
                        ActionType = "System",
                        Description = "보유 포인트 및 출석 정보 조회",
                        RequiredRole = "all"
                    });
                }
            }

            // 3. 룰렛 명령어
            var roulettes = await _db.Roulettes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            combinedList.AddRange(roulettes.Select(r => new CombinedCommandDto
            {
                Id = $"Roulette:{r.Id}",
                Keyword = r.Command,
                Type = "Roulette",
                ActionType = "System",
                Description = $"{r.Name} 실행 (비용: {r.CostPerSpin}포인트)",
                RequiredRole = "all"
            }));

            // 4. 오마카세 명령어
            var omakases = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(o => o.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            combinedList.AddRange(omakases.Select(o => new CombinedCommandDto
            {
                Id = $"Omakase:{o.Id}",
                Keyword = o.Command,
                Type = "Omakase",
                ActionType = "System",
                Description = $"{o.Name} 실행 (비용: {o.Price}치즈)",
                RequiredRole = "all"
            }));

            return Results.Ok(combinedList);
        }

        [HttpPost("/api/commands/save/{chzzkUid}")]
        public async Task<IResult> SaveCommand(string chzzkUid, [FromBody] StreamerCommand cmd)
        {
            cmd.Id = 0; 
            cmd.ChzzkUid = chzzkUid; // 🛡️ 경로상의 UID로 강제 고정

            var existing = await _db.StreamerCommands
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.ChzzkUid == chzzkUid && c.CommandKeyword == cmd.CommandKeyword);

            if (existing == null) _db.StreamerCommands.Add(cmd);
            else { existing.ActionType = cmd.ActionType; existing.Content = cmd.Content; existing.RequiredRole = cmd.RequiredRole; }
            await _db.SaveChangesAsync();
            
            await _cacheService.RefreshAsync(chzzkUid, default);
            
            return Results.Ok();
        }

        [HttpDelete("/api/commands/delete/{chzzkUid}/{idStr}")]
        public async Task<IResult> DeleteCommand(string chzzkUid, string idStr)
        {
            var parts = idStr.Split(':');
            if (parts.Length < 2) return Results.BadRequest("Invalid ID format");

            string type = parts[0];
            string idVal = parts[1];

            if (type == "Custom")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var cmd = await _db.StreamerCommands.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == chzzkUid);
                    if (cmd != null) 
                    { 
                        _db.StreamerCommands.Remove(cmd); 
                        await _db.SaveChangesAsync(); 
                        await _cacheService.RefreshAsync(chzzkUid, default);
                    }
                }
            }
            else if (type == "Profile")
            {
                var profile = await _db.StreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (profile != null)
                {
                    if (idVal == "Song") profile.SongCommand = string.Empty;
                    else if (idVal == "Attendance") profile.AttendanceCommands = string.Empty;
                    else if (idVal == "Point") profile.PointCheckCommand = string.Empty;
                    
                    await _db.SaveChangesAsync();
                }
            }
            else if (type == "Roulette")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var roulette = await _db.Roulettes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id && r.ChzzkUid == chzzkUid);
                    if (roulette != null) { _db.Roulettes.Remove(roulette); await _db.SaveChangesAsync(); }
                }
            }
            else if (type == "Omakase")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var omakase = await _db.StreamerOmakases.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id && o.ChzzkUid == chzzkUid);
                    if (omakase != null) { _db.StreamerOmakases.Remove(omakase); await _db.SaveChangesAsync(); }
                }
            }

            return Results.Ok();
        }
    }
}
