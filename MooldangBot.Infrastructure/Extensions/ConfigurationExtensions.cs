using Microsoft.Extensions.Configuration;
using DotNetEnv;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MooldangBot.Infrastructure.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// [텔로스5의 정렬]: .env 파일을 수동 파싱하여 IConfiguration에 주입하고 환경 변수를 로드합니다. (메모리 최적화 버전)
    /// </summary>
    public static IConfigurationBuilder AddCustomDotEnv(this IConfigurationBuilder builder, string[] args)
    {
        // 1. [Zero-Git] 실행 인자에서 설정 파일 경로 추출 (--env=.env.prod 등)
        var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

        // 2. [파로스의 자각]: 서버 로컬에 있는 설정 파일 탐색
        string[] potentialPaths = { 
            envPath, 
            "../" + envPath,
            Path.Combine(Directory.GetCurrentDirectory(), envPath),
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, envPath),
            Path.Combine(Directory.GetCurrentDirectory(), "MooldangBot.Api", envPath),
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MooldangBot.Api", envPath),
            "MooldangBot.Api/.env"
        };

        string? foundPath = null;
        foreach (var p in potentialPaths)
        {
            if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; }
        }

        if (foundPath != null)
        {
            Console.WriteLine($"[파로스의 자각]: 설정 파일 발견 - {foundPath}");
            
            // DotNetEnv 로드 (기존 라이브러리 방식 병행)
            Env.Load(foundPath);

            // 🛡️ [메모리 최적화]: Dictionary를 활용한 1회성 구성 주입 (Case-Insensitive)
            var envConfigs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadAllLines(foundPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                
                var split = trimmed.Split('=', 2);
                if (split.Length != 2) continue;
                
                var key = split[0].Trim();
                var val = split[1].Trim();
                
                // 값 양 끝의 따옴표 제거
                if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
                {
                    val = val.Substring(1, val.Length - 2);
                }
                
                var mappedKey = key.Replace("__", ":");
                envConfigs[mappedKey] = val;
                
                // PascalCase 변환 (레거시 대응용)
                var pascalKey = string.Join(":", mappedKey.Split(':').Select(section => 
                    string.Join("", section.Split('_', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : p))));
                
                if (pascalKey != mappedKey)
                {
                    envConfigs[pascalKey] = val;
                }
                
                System.Environment.SetEnvironmentVariable(key, val);
            }

            // 루프 종료 후 단 한 번만 메모리 컬렉션으로 추가
            if (envConfigs.Count > 0)
            {
                builder.AddInMemoryCollection(envConfigs);
            }
        }

        return builder;
    }

    public static IConfiguration ValidateMandatorySecrets(this IConfiguration config)
    {
        // 1. [오시리스의 저울]: 모든 설정값의 평면화(Flattening) 및 대소문자 무시 검색 준비
        var configDict = config.AsEnumerable().ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        // 2. 필수 검증 키 리스트 (Project Osiris 골든 리스트)
        string[] requiredKeys = [
            "ConnectionStrings:DefaultConnection",
            "JwtSettings:Secret",
            "JwtSettings:Issuer",
            "ChzzkApi:ClientId",
            "ChzzkApi:ClientSecret",
            "YouTube:ApiKey",
            "REDIS_URL",
            "BASE_DOMAIN", 
            "RABBITMQ_HOST",
            "RABBITMQ_USER",
            "RABBITMQ_PASS"
        ];

        // 3. 누락된 키 필터링 (심층 방어적 검증 적용)
        var missingKeys = new List<string>();
        foreach (var key in requiredKeys)
        {
            // [Step 1]: 표준 매핑 시도 (Case-Insensitive)
            if (configDict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val)) continue;

            // [Step 2]: [방어적 검증] 섹션 구분자(:) 및 단어 구분자(_)를 제거하고 비교
            var normalizedTarget = key.Replace(":", "").Replace("_", "").ToUpper();
            
            bool foundFallback = false;
            foreach (var kvp in configDict)
            {
                // 환경 변수 및 설정 키에서 모든 특수 구분자 제거 후 비교
                var normalizedConfigKey = kvp.Key.Replace(":", "").Replace("_", "").ToUpper();
                
                if (normalizedConfigKey == normalizedTarget && !string.IsNullOrWhiteSpace(kvp.Value)) 
                { 
                    foundFallback = true; 
                    // [오시리스의 조언]: 실제 매핑된 이름을 출력하여 디버깅을 돕습니다.
                    Console.WriteLine($"⚖️ [오시리스의 저울]: 키 매핑 보정 - '{key}' -> '{kvp.Key}' (Match Found)");
                    break; 
                }
            }

            if (!foundFallback) 
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Any())
        {
            var missingKeysString = string.Join("\n - ", missingKeys);
            
            // 보유 중인 모든 설정 키의 목록을 시각화 (디버깅 지원)
            var availableKeys = string.Join(", ", configDict.Keys.Take(20));
            
            throw new InvalidOperationException(
                $"🔥 [오시리스의 저울 - 검증 실패]: 필수 설정값이 하나 이상 누락되었습니다.\n" +
                $"[누락된 키 목록]:\n - {missingKeysString}\n\n" +
                $"[현재 로드된 키 예시]: {availableKeys}...\n\n" +
                "💡 조치 방법: .env 파일 또는 Docker 환경 변수에 해당 키가 올바르게 설정되었는지 확인하십시오."
            );
        }

        Console.WriteLine("⚖️ [오시리스의 저울]: 모든 필수 환경 변수 검증 완료. 기동을 계속합니다.");
        return config;
    }
}
