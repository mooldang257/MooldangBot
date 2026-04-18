using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MooldangBot.Application.Common.Security;
using System.Security.Claims;
using System.Text;

namespace MooldangBot.Application.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddMooldangSecurity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSecret = configuration["JwtSettings:Secret"]!;
        if (Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
        {
            throw new InvalidOperationException("❌ [Security Error]: JWT 시크릿 키가 너무 짧습니다. 최소 32바이트(256비트) 이상의 키를 사용하십시오.");
        }
        var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options => { 
            options.LoginPath = "/api/auth/chzzk-login"; 
            options.Cookie.Name = configuration["AUTH_COOKIE_NAME"] ?? ".MooldangBot.Session";
            
            var isDev = environment.IsDevelopment();
            options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        })
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JwtSettings:Issuer"] ?? "MooldangBot",
                ValidAudience = configuration["JwtSettings:Audience"] ?? "MooldangBot_Overlay",
                IssuerSigningKey = issuerSigningKey
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/overlayHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme)
                .Build();

            options.AddPolicy("ChannelManager", policy => {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ChannelManagerRequirement());
            });

            options.AddPolicy("OverlayAuth", policy => {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new OverlayTokenVersionRequirement());
            });
        });

        services.AddScoped<IAuthorizationHandler, ChannelManagerAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OverlayTokenVersionHandler>();

        return services;
    }
}
