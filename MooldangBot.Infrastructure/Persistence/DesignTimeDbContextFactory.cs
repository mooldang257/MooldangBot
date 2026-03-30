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
        // 1. .env 파일 로드 (공용 경로 탐색)
        string[] potentialPaths = { 
            ".env", 
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
            Env.Load(foundPath);
        }

        // 2. 환경 변수 기반 설정 구성
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Dev";
        var prefix = envName.ToUpper().Replace("DEVELOPMENT", "DEV") + "_";
        
        // [파로스의 자각]: 연결 문자열 추출 (대소문자 무시 검색을 위해 전체 환경변수 순회)
        var allEnv = Environment.GetEnvironmentVariables();
        string? connectionString = null;

        foreach (string key in allEnv.Keys)
        {
            if (key.Equals($"{prefix}CONNECTIONSTRINGS__DEFAULT_CONNECTION", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("CONNECTIONSTRINGS__DEFAULT_CONNECTION", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("DefaultConnection", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = allEnv[key]?.ToString();
                break;
            }
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            // 폴백 (하드코딩 방지: .env 파일이 없으면 최후의 수단으로만 사용)
            connectionString = "Server=127.0.0.1;Database=MooldangBot;User=root;Password=@enjoy1004;";
        }

        // 3. [오시리스의 중재]: Docker 환경(DB_HOST) 감지 시 서버 주소 자동 치환
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        if (!string.IsNullOrEmpty(dbHost) && connectionString.Contains("Server=127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.Replace("Server=127.0.0.1", $"Server={dbHost}", StringComparison.OrdinalIgnoreCase);
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Design-Time(빌드 시점)에는 실제 DB에 연결할 수 없으므로 AutoDetect 대신 명시적인 버전을 사용합니다.
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb"));

        // Design-time에는 인증 정보가 필요 없으므로 더미 세션 제공
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
