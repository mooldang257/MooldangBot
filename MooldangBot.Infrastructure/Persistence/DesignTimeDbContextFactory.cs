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
        // 1. [파로스의 자각]: 환경 변수 로드 (Docker/Local 공통)
        string[] potentialPaths = { ".env", "../.env", "MooldangBot.Api/.env" };
        foreach (var p in potentialPaths) { if (File.Exists(p)) { Env.Load(p); break; } }

        // 2. [오시리스의 저울]: 개별 환경 변수 우선 추출
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "127.0.0.1";
        var dbName = Environment.GetEnvironmentVariable("MARIADB_DATABASE") ?? "MooldangBot";
        var dbUser = Environment.GetEnvironmentVariable("MARIADB_USER") ?? "root";
        
        // 유저에 맞는 비밀번호를 선택합니다.
        var dbPass = (dbUser == "root")
                    ? Environment.GetEnvironmentVariable("MARIADB_ROOT_PASSWORD")
                    : Environment.GetEnvironmentVariable("MARIADB_PASSWORD");
        
        // 위에서 찾지 못했다면 상호 보완적으로 확인합니다.
        dbPass ??= Environment.GetEnvironmentVariable("MARIADB_PASSWORD") 
                    ?? Environment.GetEnvironmentVariable("MARIADB_ROOT_PASSWORD")
                    ?? "enjoy1004";

        // 따옴표(") 제거 (방어적 코드)
        dbHost = dbHost.Trim('\"');
        dbName = dbName.Trim('\"');
        dbUser = dbUser.Trim('\"');
        dbPass = dbPass.Trim('\"', ';'); // 세미콜론 포함 가능성 방어

        // 3. [디버깅 로그]: 접속 시도 정보 출력 (마스킹)
        string Mask(string s) => s.Length > 2 ? s[..2] + "****" : "****";
        Console.WriteLine($"\n🔍 [DesignTimeFactory]: 접속 시도 중...");
        Console.WriteLine($"   📍 Host: {dbHost} / DB: {dbName}");
        Console.WriteLine($"   👤 User: {dbUser} / Pwd: {Mask(dbPass)}");

        // 4. [접속 문자열 완성]: 개별 변수를 조합하여 파싱 오류 차단
        var connectionString = $"Server={dbHost};Port=3306;Database={dbName};Uid={dbUser};Pwd={dbPass};SslMode=None;AllowUserVariables=True;";

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
