using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.ApiClients;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Interfaces;

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

            return services;
        }
    }
}
