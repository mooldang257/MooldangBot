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
        // [물멍]: 단일 API 버전 체계로의 통합을 위해 Asp.Versioning 설정을 제거합니다.
        // 모든 엔드포인트는 이제 /api/ 경로를 기본으로 사용합니다.

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(IAppDbContext).Assembly);

        return services;
    }
}
