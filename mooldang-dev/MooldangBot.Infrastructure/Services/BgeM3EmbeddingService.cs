using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Contracts.AI.Interfaces;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [물멍]: BGE-M3 모델을 사용하는 로컬 임베딩 서비스 구현체입니다.
/// </summary>
public class BgeM3EmbeddingService : IVectorEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public BgeM3EmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        
        // [오시리스의 지혜]: Docker 환경과 로컬 개발 환경 모두에서 작동하도록 설정에서 URL을 가져옵니다.
        // 기본값은 Docker 네트워크 내부 주소입니다.
        _baseUrl = configuration["AI:EMBEDDING_SERVER_URL"] ?? "http://mooldang-dev-embeddings";
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return new float[1024];

            // [HuggingFace TEI API]: /embed 엔드포인트에 입력을 보냅니다.
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/embed", new { inputs = text });
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Embedding] API 호출 실패: {response.StatusCode}, {error}");
                return new float[1024];
            }

            // TEI는 단일 입력 시 [f1, f2, ...] 형태의 배열을 반환합니다.
            var result = await response.Content.ReadFromJsonAsync<float[]>();
            return result ?? new float[1024];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Embedding] 오류 발생: {ex.Message}");
            return new float[1024];
        }
    }

    public string ToVectorString(float[] vector)
    {
        // MariaDB VEC_FROMTEXT는 [1.0, 2.0, ...] 형식의 JSON 배열 문자열을 인식합니다.
        return JsonSerializer.Serialize(vector);
    }
}
