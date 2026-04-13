using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Persistence;

public class DbInitializer(
    AppDbContext db, 
    IConfiguration config,
    MooldangBot.Contracts.Common.Interfaces.IChzzkChatClient chatClient,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        logger.LogInformation("🚀 [오시리스의 시동] 시스템 초기화 서비스를 시작합니다.");

        try
        {
            // 1. 데이터베이스 마이그레이션 적용 (스키마 자동 생성/변경)
            logger.LogInformation("🛠️ [오시리스의 시동] 데이터베이스 마이그레이션을 적용 중...");
            await db.Database.MigrateAsync();

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
}
