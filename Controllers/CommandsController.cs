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
            return Results.Ok(await _db.StreamerCommands.Where(c => c.ChzzkUid == chzzkUid).ToListAsync());
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

        [HttpDelete("/api/commands/delete/{id}")]
        public async Task<IResult> DeleteCommand(int id)
        {
            var cmd = await _db.StreamerCommands.FindAsync(id);
            if (cmd != null) { _db.StreamerCommands.Remove(cmd); await _db.SaveChangesAsync(); }
            return Results.Ok();
        }
    }
}
