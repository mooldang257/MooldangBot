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
            
            // [오시리스의 도서관]: 공용 썸네일 도서관 DB 초기화
            logger.LogInformation("📚 [오시리스의 시동] 공용 썸네일 도서관 DB를 초기화 중...");
            await commonDb.Database.EnsureCreatedAsync(); 
            logger.LogInformation("✅ [오시리스의 시동] 공용 도서관 준비 완료.");

            // 2. [v23.0] 오시리스의 예지: 하이브리드 검색 인프라(인덱스) 구축
            logger.LogInformation("🧠 [오시리스의 시동] 하이브리드 검색 인덱스 상태 점검 중...");
            
            // GlobalMusicMetadata 테이블 및 인덱스 (Dapper 기반 테이블이므로 수동 생성)
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS GlobalMusicMetadata (
                    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    Artist VARCHAR(255),
                    Title VARCHAR(255),
                    ThumbnailUrl TEXT,
                    LyricsUrl TEXT,
                    MetadataVector VECTOR(1024),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FULLTEXT INDEX ft_music (Artist, Title)
                ) ENGINE=InnoDB;
            ");

            // [오시리스의 시동]: 하이브리드 검색 인프라 최적화 (전체 라이브러리 대상)
            try {
                // 1. FuncSongBooks 최적화
                await db.Database.ExecuteSqlRawAsync("CREATE FULLTEXT INDEX IF NOT EXISTS ft_songbook ON FuncSongBooks (Artist, Title);");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE FuncSongBooks MODIFY COLUMN TitleVector VECTOR(1024);");

                // 2. FuncSongMasterLibrary 최적화
                await db.Database.ExecuteSqlRawAsync("CREATE FULLTEXT INDEX IF NOT EXISTS ft_master ON FuncSongMasterLibrary (Artist, Title);");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE FuncSongMasterLibrary MODIFY COLUMN TitleVector VECTOR(1024);");

                // 3. FuncSongStreamerLibrary 최적화
                await db.Database.ExecuteSqlRawAsync("CREATE FULLTEXT INDEX IF NOT EXISTS ft_streamer_lib ON FuncSongStreamerLibrary (Artist, Title);");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE FuncSongStreamerLibrary MODIFY COLUMN TitleVector VECTOR(1024);");

                // 4. FuncSongMasterStaging 최적화
                await db.Database.ExecuteSqlRawAsync("CREATE FULLTEXT INDEX IF NOT EXISTS ft_staging ON FuncSongMasterStaging (Artist, Title);");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE FuncSongMasterStaging MODIFY COLUMN TitleVector VECTOR(1024);");
            } catch (System.Exception ex) { 
                logger.LogWarning("⚠️ [오시리스의 경고] 일부 인프라 최적화 스킵: {Message}", ex.Message); 
            }

            logger.LogInformation("✅ [오시리스의 시동] 하이브리드 검색 인프라 전수 최적화 완료.");

            // 3. 초기 기동 서비스 실행 (치지직 채팅 클라이언트 등)
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
