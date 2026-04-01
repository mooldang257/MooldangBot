using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using MooldangBot.Domain.Common;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SharedComponentController : ControllerBase
    {
        private readonly IAppDbContext _db;

        public SharedComponentController(IAppDbContext db)
        {
            _db = db;
        }

        private async Task<string?> GetCurrentChzzkUidAsync()
        {
            return await Task.FromResult(User.FindFirstValue("StreamerId"));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SharedComponentDto>>> GetComponents()
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var components = await _db.SharedComponents
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

            return Ok(components);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SharedComponentDto>> GetComponent(int id)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var component = await _db.SharedComponents
                .AsNoTracking()
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);

            if (component == null) return NotFound();

            return Ok(new SharedComponentDto
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type,
                ConfigJson = component.ConfigJson
            });
        }

        [HttpPost]
        public async Task<ActionResult<SharedComponentDto>> CreateComponent(SharedComponentDto dto)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return NotFound("Profile not found");

            var component = new SharedComponent
            {
                StreamerProfileId = profile.Id,
                Name = dto.Name,
                Type = dto.Type,
                ConfigJson = dto.ConfigJson,
                CreatedAt = KstClock.Now
            };

            _db.SharedComponents.Add(component);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComponent), new { id = component.Id }, new SharedComponentDto
            {
                Id = component.Id,
                Name = component.Name,
                Type = component.Type,
                ConfigJson = component.ConfigJson
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, SharedComponentDto dto)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var component = await _db.SharedComponents
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);
            if (component == null) return NotFound();

            component.Name = dto.Name;
            component.Type = dto.Type;
            component.ConfigJson = dto.ConfigJson;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var component = await _db.SharedComponents
                .Include(c => c.StreamerProfile)
                .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == chzzkUid);
            if (component == null) return NotFound();

            _db.SharedComponents.Remove(component);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
