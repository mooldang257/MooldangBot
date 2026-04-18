using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Contracts.Common.Interfaces;

namespace MooldangBot.Application.Extensions;

public static class DbExtensions
{
    public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }
}
