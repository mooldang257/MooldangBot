using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
    var streamers = await db.StreamerProfiles.AsNoTracking().IgnoreQueryFilters().ToListAsync();
    
    Console.WriteLine($"Total Streamers: {streamers.Count}");
    foreach (var s in streamers)
    {
        Console.WriteLine($"UID: {s.ChzzkUid}, Name: {s.ChannelName}, Image: {s.ProfileImageUrl ?? "NULL"}");
    }
}
