using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Persistence;

public class DbInitializer(
    AppDbContext db, 
    IConfiguration config,
    IChzzkChatClient chatClient,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        logger.LogInformation("🚀 [오시리스의 시동] 시스템 초기화 서비스를 시작합니다.");

        try
        {
            // 1. 필수 시스템 설정 동기화 (비동기 처리)
            await EnsureSettingAsync("ChzzkClientId", config["CHZZK_API:CLIENT_ID"] ?? config["ChzzkApi:ClientId"]);
            await EnsureSettingAsync("ChzzkClientSecret", config["CHZZK_API:CLIENT_SECRET"] ?? config["ChzzkApi:ClientSecret"]);

            await db.SaveChangesAsync();
            logger.LogInformation("✅ [오시리스의 시동] 마스터 설정값 동기화 완료.");

            // 2. 초기 기동 서비스 실행 (치지직 채팅 클라이언트 등)
            logger.LogInformation("📡 [오시리스의 시동] 치지직 채팅 클라이언트 초기화를 시작합니다.");
            await chatClient.InitializeAsync();
        }
        catch (System.Exception ex)
        {
            logger.LogCritical(ex, "🔥 [오시리스의 시동] 시스템 초기화 중 오류가 발생했습니다.");
            throw;
        }
    }

    /// <summary>
    /// [오시리스의 무결성]: 특정 설정값이 존재하지 않으면 추가하고, 존재하면 최신값으로 업데이트합니다. (비동기)
    /// </summary>
    private async Task EnsureSettingAsync(string key, string? val)
    {
        if (string.IsNullOrEmpty(val)) return;

        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.KeyName == key);
        if (setting == null)
        {
            db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = val });
        }
        else
        {
            setting.KeyValue = val;
        }
    }
}
