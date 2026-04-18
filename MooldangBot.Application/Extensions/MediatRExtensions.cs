using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MooldangBot.Application.Extensions;

public static class MediatRExtensions
{
    public static IServiceCollection AddMooldangMediatR(this IServiceCollection services)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        if (executionPath != null)
        {
            foreach (var dll in Directory.GetFiles(executionPath, "MooldangBot.*.dll"))
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dll);
                    if (loadedAssemblies.All(a => a.FullName != assemblyName.FullName))
                    {
                        loadedAssemblies.Add(Assembly.Load(assemblyName));
                    }
                }
                catch { /* Ignore load errors */ }
            }
        }

        var finalAssemblies = loadedAssemblies
            .Where(a => a.FullName != null && a.FullName.StartsWith("MooldangBot"))
            .ToArray();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(finalAssemblies);
        });

        return services;
    }
}
