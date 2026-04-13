using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 지혜]: MariaDB를 활용한 영구 개인화 설정 서비스의 실무 구현체입니다.
/// </summary>
public class PreferenceDbService(IAppDbContext context) : IPreferenceDbService
{
    public async Task SetPermanentPreferenceAsync(string chzzkUid, string key, string value)
    {
        var profile = await context.StreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

        if (profile == null) return;

        var preference = await context.StreamerPreferences
            .FirstOrDefaultAsync(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == key);

        if (preference == null)
        {
            preference = new StreamerPreference
            {
                StreamerProfileId = profile.Id,
                PreferenceKey = key,
                PreferenceValue = value
            };
            context.StreamerPreferences.Add(preference);
        }
        else
        {
            preference.PreferenceValue = value;
        }

        await context.SaveChangesAsync();
    }

    public async Task<string?> GetPermanentPreferenceAsync(string chzzkUid, string key)
    {
        var profile = await context.StreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

        if (profile == null) return null;

        var preference = await context.StreamerPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == key);

        return preference?.PreferenceValue;
    }

    public async Task RemovePermanentPreferenceAsync(string chzzkUid, string key)
    {
        var profile = await context.StreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

        if (profile == null) return;

        var preference = await context.StreamerPreferences
            .FirstOrDefaultAsync(p => p.StreamerProfileId == profile.Id && p.PreferenceKey == key);

        if (preference != null)
        {
            context.StreamerPreferences.Remove(preference);
            await context.SaveChangesAsync();
        }
    }
}
