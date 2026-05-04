using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using StackExchange.Redis;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 영속]: IVectorEmbeddingService의 결과를 Redis에 캐싱하는 데코레이터입니다.
/// CPU 부하를 줄이고 응답 속도를 극대화합니다.
/// </summary>
public class CachedVectorEmbeddingService : IVectorEmbeddingService
{
    private readonly IVectorEmbeddingService _inner;
    private readonly IDatabase _cache;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(7); // 임베딩은 모델 변경 전까지 불변

    public CachedVectorEmbeddingService(IVectorEmbeddingService inner, IConnectionMultiplexer redis)
    {
        _inner = inner;
        _cache = redis.GetDatabase();
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new float[1024];

        var key = GetCacheKey(text);
        
        // 1. Redis에서 시도
        try
        {
            var cached = await _cache.StringGetAsync(key);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<float[]>(cached!) ?? new float[1024];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redis] 캐시 조회 실패 (Fallback 진행): {ex.Message}");
        }

        // 2. 캐시 없으면 원본 서비스 호출
        var vector = await _inner.GetEmbeddingAsync(text);

        // 3. Redis에 저장 (Fire & Forget)
        if (vector != null && vector.Length > 0)
        {
            try
            {
                var json = JsonSerializer.Serialize(vector);
                _ = _cache.StringSetAsync(key, json, CacheExpiry, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis] 캐시 저장 실패: {ex.Message}");
            }
        }

        return vector ?? new float[1024];
    }

    public string ToVectorString(float[] vector) => _inner.ToVectorString(vector);

    private string GetCacheKey(string text)
    {
        // 텍스트가 길 수 있으므로 MD5 해시로 키 생성
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return $"emb:{Convert.ToHexString(hashBytes)}";
    }
}
