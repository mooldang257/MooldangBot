using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using System.Reflection;

namespace MooldangBot.Application.Extensions;

public static class SerilogExtensions
{
    public static void AddMooldangLogging(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => {
            var lokiUrl = context.Configuration["LOKI_URL"] ?? "http://localhost:3100";
            var instanceId = context.Configuration["INSTANCE_ID"] ?? "osiris-unknown";
            var env = context.HostingEnvironment.EnvironmentName;
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
                .Filter.ByExcluding("SourceContext = 'Microsoft.EntityFrameworkCore.Database.Command' and @Level = 'Information'")
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("InstanceId", instanceId)
                .Enrich.WithProperty("Environment", env)
                .Enrich.WithProperty("Version", version)
                .WriteTo.Async(a => a.Console())
                .WriteTo.Async(a => a.File("logs/mooldangbot-.log", rollingInterval: RollingInterval.Day))
                .WriteTo.Async(a => a.GrafanaLoki(lokiUrl, new[] 
                { 
                    new LokiLabel { Key = "app", Value = "mooldangbot" },
                    new LokiLabel { Key = "instance", Value = instanceId },
                    new LokiLabel { Key = "machine", Value = Environment.MachineName },
                    new LokiLabel { Key = "env", Value = env },
                    new LokiLabel { Key = "version", Value = version }
                }));
        });
    }
}
