using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using MooldangBot.Domain.Common;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.Shared
{
    [ApiController]
    [Route("api/SharedComponent")]
    [Authorize]
    // [v10.1] Primary Constructor 적용
    public class SharedComponentController(IAppDbContext db) : ControllerBase
    {
        private string? GetCurrentChzzkUid()
        {
            return User.FindFirstValue("StreamerId");
        }

        [HttpGet]
        public async Task<IActionResult> GetComponents()
        {
            var chzzkUid = GetCurrentChzzkUid();
            if (string.IsNullOrEmpty(chzzkUid)) 
                return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

            var components = await db.SharedComponents
                .AsNoTracking()
                .Include(c => c.StreamerProfile)
                .Where(c => c.StreamerProfile!.ChzzkUid == chzzkUid)
                .Select(c => new SharedComponentDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    ConfigJson = c.ConfigJson
                })
                .ToListAsync();

            return Ok(Result<List<SharedComponentDto>>.Success(components));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComponent(int id)
        {
            var chzzkUid = GetCurrentChzzkUid();
            if (string.IsNullOrEmpty(chzzkUid)) 
                return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

            var component = await db.SharedComponents
                .AsNoTracking()
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);

            if (component == null) 
                return NotFound(Result<string>.Failure("컴포넌트를 찾을 수 없습니다."));

            return Ok(Result<SharedComponentDto>.Success(new SharedComponentDto
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type,
                ConfigJson = component.ConfigJson
            }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateComponent(SharedComponentDto dto)
        {
            var chzzkUid = GetCurrentChzzkUid();
            if (string.IsNullOrEmpty(chzzkUid)) 
                return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
                return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

            var component = new SharedComponent
            {
                StreamerProfileId = profile.Id,
                Name = dto.Name,
                Type = dto.Type,
                ConfigJson = dto.ConfigJson,
                CreatedAt = KstClock.Now
            };

            db.SharedComponents.Add(component);
            await db.SaveChangesAsync();

            return Ok(Result<SharedComponentDto>.Success(new SharedComponentDto
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type,
                ConfigJson = component.ConfigJson
            }));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, SharedComponentDto dto)
        {
            var chzzkUid = GetCurrentChzzkUid();
            if (string.IsNullOrEmpty(chzzkUid)) 
                return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

            var component = await db.SharedComponents
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);
            
            if (component == null) 
                return NotFound(Result<string>.Failure("컴포넌트를 찾을 수 없습니다."));

            component.Name = dto.Name;
            component.Type = dto.Type;
            component.ConfigJson = dto.ConfigJson;

            await db.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "컴포넌트가 업데이트되었습니다." }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var chzzkUid = GetCurrentChzzkUid();
            if (string.IsNullOrEmpty(chzzkUid)) 
                return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

            var component = await db.SharedComponents
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);
            
            if (component == null) 
                return NotFound(Result<string>.Failure("컴포넌트를 찾을 수 없습니다."));

            db.SharedComponents.Remove(component);
            await db.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "컴포넌트가 삭제되었습니다." }));
        }
    }
}
