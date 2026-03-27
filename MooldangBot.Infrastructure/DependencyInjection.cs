using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Infrastructure.ApiClients.Philosophy;

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

            return services;
        }
    }
}
