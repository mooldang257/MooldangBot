using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Persistence;

public class DbInitializer(
    AppDbContext db, 
    CommonDbContext commonDb, // [v19.5] 공용 DB 추가
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
            // [v11.8-Fix]: API 컨테이너에서의 마이그레이션 중복 실행이 SEGFAULT를 유발하므로 
            // 운영 환경에서는 CLI 컨테이너에게 이 역할을 위임하고 앱에서는 생략합니다.
            // logger.LogInformation("🛠️ [오시리스의 시동] 데이터베이스 마이그레이션을 적용 중...");
            // await db.Database.MigrateAsync();

            // [오시리스의 도서관]: 공용 썸네일 도서관 DB 초기화
            logger.LogInformation("📚 [오시리스의 시동] 공용 썸네일 도서관 DB를 초기화 중...");
            await commonDb.Database.EnsureCreatedAsync(); 
            logger.LogInformation("✅ [오시리스의 시동] 공용 도서관 준비 완료.");

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
