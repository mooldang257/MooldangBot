using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Security.Claims;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SharedComponentController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SharedComponentController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<string?> GetCurrentChzzkUidAsync()
        {
            var naverId = User.FindFirstValue("StreamerId");
            if (string.IsNullOrEmpty(naverId)) return null;

            var profile = await _db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.NaverId == naverId);
            return profile?.ChzzkUid;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SharedComponentDto>>> GetComponents()
        {
            var chzzkUid = await GetCurrentChzzkUidAsync();
            if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

            var components = await _db.SharedComponents
                .AsNoTracking()
                .Where(c => c.ChzzkUid == chzzkUid)
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
                .FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == chzzkUid);

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

            var component = new SharedComponent
            {
                ChzzkUid = chzzkUid,
                Name = dto.Name,
                Type = dto.Type,
                ConfigJson = dto.ConfigJson,
                CreatedAt = DateTime.UtcNow
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

            var component = await _db.SharedComponents.FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == chzzkUid);
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

            var component = await _db.SharedComponents.FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == chzzkUid);
            if (component == null) return NotFound();

            _db.SharedComponents.Remove(component);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
