using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 36)); 

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var profiles = await db.StreamerProfiles.ToListAsync();
foreach (var p in profiles)
{
    Console.WriteLine($"ID: {p.Id}, Name: {p.ChannelName}, Uid: {p.ChzzkUid}, IsBotEnabled: {p.IsBotEnabled}, HasAccessToken: {!string.IsNullOrEmpty(p.ChzzkAccessToken)}");
}
