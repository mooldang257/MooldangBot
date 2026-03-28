using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Infrastructure.ApiClients.Philosophy;
using MooldangBot.Application.Services.Philosophy;
using MooldangBot.Infrastructure.Services;

namespace MooldangBot.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(configuration.GetConnectionString("DefaultConnection"), 
                    ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))));
            
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Api Clients
            services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>();
            
            // [거울의 신경망]: Gemini API 실전 연동
            services.AddHttpClient<ILlmService, MooldangBot.Infrastructure.ApiClients.Philosophy.GeminiLlmService>();

            // [피닉스의 심장]: 실전 채팅 클라이언트
            services.AddSingleton<IChzzkChatClient, MooldangBot.Infrastructure.ApiClients.Philosophy.ChzzkChatClient>();

            // [오시리스의 기록관]: 방송 통계 및 세션 관리
            services.AddSingleton<IBroadcastScribe, BroadcastScribe>();

            // [v1.2] 마스터 데이터 캐시 서비스 등록
            services.AddScoped<ICommandMasterCacheService, CommandMasterCacheService>();

            // [v1.8] Safe Dynamic Query Engine 등록
            services.AddScoped<IDynamicQueryEngine, MooldangBot.Infrastructure.Services.Engines.DynamicQueryEngine>();

            // [v4.4.0] Dynamic Variable Resolver 등록
            services.AddScoped<IDynamicVariableResolver, MooldangBot.Infrastructure.Services.Engines.DynamicVariableResolver>();

            return services;
        }
    }
}
