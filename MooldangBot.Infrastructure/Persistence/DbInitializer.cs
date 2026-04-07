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
