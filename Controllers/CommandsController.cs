using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class CommandsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CommandsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/api/commands/list/{chzzkUid}")]
        public async Task<IResult> GetCommands(string chzzkUid)
        {
            var combinedList = new List<CombinedCommandDto>();
            var targetUid = chzzkUid.Trim().ToLower();

            // 1. 일반 커스텀 명령어 (StreamerCommands)
            var customCmds = await _db.StreamerCommands
                .AsNoTracking()
                .Where(c => c.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
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
            var omakases = await _db.StreamerOmakaseItems
                .AsNoTracking()
                .Where(o => o.ChzzkUid.ToLower() == targetUid)
                .ToListAsync();
                
            combinedList.AddRange(omakases.Select(o => new CombinedCommandDto
            {
                Id = $"Omakase:{o.Id}",
                Keyword = o.Command,
                Type = "Omakase",
                ActionType = "System",
                Description = $"{o.Name} 실행 (비용: {o.CheesePrice}치즈)",
                RequiredRole = "all"
            }));

            return Results.Ok(combinedList);
        }

        [HttpPost("/api/commands/save")]
        public async Task<IResult> SaveCommand([FromBody] StreamerCommand cmd)
        {
            var existing = await _db.StreamerCommands.FirstOrDefaultAsync(c => c.ChzzkUid == cmd.ChzzkUid && c.CommandKeyword == cmd.CommandKeyword);
            if (existing == null) _db.StreamerCommands.Add(cmd);
            else { existing.ActionType = cmd.ActionType; existing.Content = cmd.Content; existing.RequiredRole = cmd.RequiredRole; }
            await _db.SaveChangesAsync();
            return Results.Ok();
        }

        [HttpDelete("/api/commands/delete/{idStr}")]
        public async Task<IResult> DeleteCommand(string idStr)
        {
            var parts = idStr.Split(':');
            if (parts.Length < 2) return Results.BadRequest("Invalid ID format");

            string type = parts[0];
            string idVal = parts[1];

            if (type == "Custom")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var cmd = await _db.StreamerCommands.FindAsync(id);
                    if (cmd != null) { _db.StreamerCommands.Remove(cmd); await _db.SaveChangesAsync(); }
                }
            }
            else if (type == "Profile")
            {
                var userChzzkUid = User.FindFirstValue("StreamerId");
                if (!string.IsNullOrEmpty(userChzzkUid))
                {
                    var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == userChzzkUid);
                    if (profile != null)
                    {
                        if (idVal == "Song") profile.SongCommand = string.Empty;
                        else if (idVal == "Attendance") profile.AttendanceCommands = string.Empty;
                        else if (idVal == "Point") profile.PointCheckCommand = string.Empty;
                        
                        await _db.SaveChangesAsync();
                    }
                }
            }
            else if (type == "Roulette")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var roulette = await _db.Roulettes.FindAsync(id);
                    if (roulette != null) { _db.Roulettes.Remove(roulette); await _db.SaveChangesAsync(); }
                }
            }
            else if (type == "Omakase")
            {
                if (int.TryParse(idVal, out int id))
                {
                    var omakase = await _db.StreamerOmakaseItems.FindAsync(id);
                    if (omakase != null) { _db.StreamerOmakaseItems.Remove(omakase); await _db.SaveChangesAsync(); }
                }
            }

            return Results.Ok();
        }
    }
}
