using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Abstractions;
using System.Text.Json;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [?ㅻ쾭?뚯튂: ?덉씠釉??낅뜲?댄듃]: ?ㅻ쾭?덉씠???ㅼ뼵??而ㅼ뒪? ?덉씠釉??ㅼ젙???낅뜲?댄듃?⑸땲??
/// </summary>
public record UpdateLabelsCommand(string StreamerUid, JsonElement Labels) : IRequest<Result<object>>;

public class UpdateLabelsHandler(ISongBookDbContext db) : IRequestHandler<UpdateLabelsCommand, Result<object>>
{
    public async Task<Result<object>> Handle(UpdateLabelsCommand request, CancellationToken ct)
    {
        var targetUid = request.StreamerUid.ToLower();
        var profile = await db.CoreStreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted, ct);
        
        if (profile == null) 
            return Result<object>.Failure("議댁옱?섏? ?딅뒗 梨꾨꼸?낅땲??");

        var options = new JsonSerializerOptions { WriteIndented = true };
        var designData = string.IsNullOrEmpty(profile.DesignSettingsJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(profile.DesignSettingsJson) ?? new Dictionary<string, object>();

        designData["Labels"] = request.Labels;
        profile.DesignSettingsJson = JsonSerializer.Serialize(designData, options);
        
        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { success = true });
    }
}
