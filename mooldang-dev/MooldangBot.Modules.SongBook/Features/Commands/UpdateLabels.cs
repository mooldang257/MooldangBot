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
        var TargetUid = request.StreamerUid.ToLower();
        var Profile = await db.TableCoreStreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == TargetUid && !p.IsDeleted, ct);
        
        if (Profile == null) 
            return Result<object>.Failure("議댁옱?섏? ?딅뒗 梨꾨꼸?낅땲??");
 
        var Options = new JsonSerializerOptions { WriteIndented = true };
        var DesignData = string.IsNullOrEmpty(Profile.DesignSettingsJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Profile.DesignSettingsJson) ?? new Dictionary<string, object>();
 
        DesignData["Labels"] = request.Labels;
        Profile.DesignSettingsJson = JsonSerializer.Serialize(DesignData, Options);
        
        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { Success = true });
    }
}
