using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Abstractions;

namespace MooldangBot.Application.Extensions;

public static class VersioningExtensions
{
    public static IServiceCollection AddMooldangVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version")
            );
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(IAppDbContext).Assembly);

        return services;
    }
}
