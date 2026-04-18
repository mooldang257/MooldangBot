using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Persistence;

public class DbInitializer(
    AppDbContext db, 
    IConfiguration config,
    MooldangBot.Domain.Abstractions.IChzzkChatClient chatClient,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        logger.LogInformation("🚀 [오시리스의 시동] 시스템 초기화 서비스를 시작합니다.");

        try
        {
            // [v7.1-Fix] 오시리스의 세척: RESET_DATABASE 환경변수가 true일 경우 DB를 완전히 초기화합니다.
            if (config.GetValue<bool>("RESET_DATABASE"))
            {
                logger.LogWarning("⚠️ [오시리스의 시동] RESET_DATABASE=true 감지. 데이터베이스를 초기화(EnsureDeleted)합니다.");
                await db.Database.EnsureDeletedAsync();
                logger.LogInformation("✅ [오시리스의 시동] 데이터베이스가 완전히 삭제되었습니다.");
            }

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
