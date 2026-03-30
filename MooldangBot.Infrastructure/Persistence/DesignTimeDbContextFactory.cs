using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MooldangBot.Application.Interfaces;
using DotNetEnv;

namespace MooldangBot.Infrastructure.Persistence;

/// <summary>
/// EF Core 디자인 타임 도구(dotnet ef)를 위한 DbContext 팩토리입니다.
/// 가동 중인 호스트 없이도 마이그레이션을 생성하고 적용할 수 있게 돕습니다.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1. [파로스의 자각]: .env 파일 탐색 (루트 폴더 포함)
        string[] potentialPaths = { 
            ".env", 
            "../.env", // ⬅️ 루트 탐색 추가
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "MooldangBot.Api", ".env"),
            "MooldangBot.Api/.env"
        };

        string? foundPath = null;
        foreach (var p in potentialPaths)
        {
            if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; }
        }

        if (foundPath != null)
        {
            // .env 파일을 직접 파싱하거나 Env.Load를 사용합니다.
            Env.Load(foundPath);
        }

        // 2. [오시리스의 저울]: 유연한 연결 문자열 추출 (환경 변수 우선)
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var prefix = envName.ToUpper().Replace("DEVELOPMENT", "DEV").Replace("PRODUCTION", "PROD") + "_";
        
        string? connectionString = null;
        var allEnv = Environment.GetEnvironmentVariables();

        // 탐색 우선순위 정의
        string[] connectionKeys = {
            $"{prefix}ConnectionStrings__DefaultConnection",
            $"{prefix}CONNECTIONSTRINGS__DEFAULT_CONNECTION",
            "ConnectionStrings__DefaultConnection",
            "CONNECTIONSTRINGS__DEFAULT_CONNECTION",
            "DefaultConnection",
            "DEFAULT_CONNECTION"
        };

        foreach (var key in connectionKeys)
        {
            foreach (string envKey in allEnv.Keys)
            {
                if (envKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    connectionString = allEnv[envKey]?.ToString();
                    break;
                }
            }
            if (!string.IsNullOrEmpty(connectionString)) break;
        }

        // 3. [방어적 프로그래밍]: 따옴표(") 제거 및 DB_HOST 치환
        if (!string.IsNullOrEmpty(connectionString))
        {
            // .env에서 읽어올 때 포함될 수 있는 중복 따옴표 제거
            connectionString = connectionString.Trim('\"');
            
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            if (!string.IsNullOrEmpty(dbHost) && (connectionString.Contains("Server=localhost") || connectionString.Contains("Server=127.0.0.1")))
            {
                connectionString = connectionString
                    .Replace("Server=localhost", $"Server={dbHost}", StringComparison.OrdinalIgnoreCase)
                    .Replace("Server=127.0.0.1", $"Server={dbHost}", StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            // 폴백 (하드코딩 방지: 가급적 .env를 참조하게 유도)
            connectionString = "Server=db;Database=MooldangBot;User=root;Password=@enjoy1004;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb"));

        return new AppDbContext(optionsBuilder.Options, new DesignTimeUserSession());
    }

    private class DesignTimeUserSession : IUserSession
    {
        public bool IsAuthenticated => false;
        public string? ChzzkUid => null;
        public string? Role => null;
        public List<string> AllowedChannelIds => new List<string>();
    }
}
