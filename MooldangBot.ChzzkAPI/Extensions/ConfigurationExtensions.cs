using Microsoft.Extensions.Configuration;
using DotNetEnv;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MooldangBot.ChzzkAPI.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// [오시리스의 격리]: .env 파일을 수동 파싱하여 IConfiguration에 주입합니다. (게이트웨이 독립 버전)
    /// </summary>
    public static IConfigurationBuilder AddCustomDotEnv(this IConfigurationBuilder builder, string[] args)
    {
        var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

        string[] potentialPaths = { 
            envPath, 
            "../" + envPath,
            Path.Combine(Directory.GetCurrentDirectory(), envPath),
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, envPath)
        };

        string? foundPath = null;
        foreach (var p in potentialPaths)
        {
            if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; }
        }

        if (foundPath != null)
        {
            Console.WriteLine($"⚖️ [오시리스의 자각]: 게이트웨이 설정 파일 발견 - {foundPath}");
            
            Env.Load(foundPath);
            Console.WriteLine($"✅ [오시리스의 자각]: .env 파일 로드 완료");
        }
        else
        {
            // [오시리스의 자각]: .env 파일이 없어도 시스템 환경 변수에 의존하므로 경고를 띄우지 않습니다.
        }

        var envConfigs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (foundPath != null)
        {
            foreach (var line in File.ReadAllLines(foundPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                
                var split = trimmed.Split('=', 2);
                if (split.Length != 2) continue;
                
                var key = split[0].Trim();
                var val = split[1].Trim();
                
                if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
                {
                    val = val.Substring(1, val.Length - 2);
                }
                
                var mappedKey = key.Replace("__", ":");
                envConfigs[mappedKey] = val;
                
                // PascalCase 변환 (CHZZKAPI__CLIENTID -> ChzzkApi:ClientId 호환용)
                var pascalKey = string.Join(":", mappedKey.Split(':').Select(section => 
                    string.Join("", section.Split('_', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : p))));
                
                if (pascalKey != mappedKey)
                {
                    envConfigs[pascalKey] = val;
                }
                
                System.Environment.SetEnvironmentVariable(key, val);
            }

            if (envConfigs.Count > 0)
            {
                builder.AddInMemoryCollection(envConfigs);
            }
        }

        return builder;
    }
}
