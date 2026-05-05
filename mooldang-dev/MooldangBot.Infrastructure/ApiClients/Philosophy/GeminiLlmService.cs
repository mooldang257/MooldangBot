using MooldangBot.Domain.Contracts.AI.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces;

namespace MooldangBot.Infrastructure.ApiClients.Philosophy;

/// <summary>
/// [거울의 신경망]: Google Gemini API를 통해 실제 인공지능 응답 및 임베딩을 생성하는 실전 구현체입니다.
/// </summary>
public class GeminiLlmService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<GeminiLlmService> logger) : ILlmService
{
    private readonly string _apiKey = config["GEMINI_KEY"] ?? config["Gemini:ApiKey"] ?? string.Empty;
    private const string GenerateUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={0}";
    private const string EmbeddingUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/{0}:embedContent?key={1}";

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("[IAMF 경고] Gemini API 키가 설정되지 않았습니다. 기본 답변으로 대체합니다.");
            return "[지혜의 부재] 현재 신경망 연결이 끊겨 있어 깊은 대화가 어렵습니다.";
        }
 
        try
        {
            var RequestBody = new
            {
                contents = new[]
                {
                    new { 
                        role = "user",
                        parts = new[] { new { text = $"{systemPrompt}\n\n[사용자 메시지]: {userMessage}" } } 
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 300
                }
            };
 
            var ApiUrl = string.Format(GenerateUrlTemplate, _apiKey);
            var Response = await httpClient.PostAsJsonAsync(ApiUrl, RequestBody);
 
            if (!Response.IsSuccessStatusCode)
            {
                var ErrorMsg = await Response.Content.ReadAsStringAsync();
                logger.LogError("[Gemini] API 호출 실패: {StatusCode}, {ErrorMsg}", Response.StatusCode, ErrorMsg);
                return string.Empty;
            }
 
            var JsonDoc = await Response.Content.ReadFromJsonAsync<JsonDocument>();
            logger.LogInformation("[Gemini] API 응답 수신 성공");
            
            var AiText = JsonDoc?.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
 
            return AiText?.Trim() ?? string.Empty;
        }
        catch (Exception Ex)
        {
            logger.LogError(Ex, "[Gemini] 연동 중 예외 발생");
            return string.Empty;
        }
    }

    /// <summary>
    /// [v18.0] 텍스트를 고차원 벡터로 변환합니다. (MariaDB 11.7 벡터 검색용)
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrWhiteSpace(text))
            return Array.Empty<float>();
 
        try
        {
            var ModelName = "models/gemini-embedding-001"; // [v19.0] 3072차원을 지원하는 텍스트 전용 임베딩 모델
            var RequestBody = new
            {
                model = ModelName,
                content = new { parts = new[] { new { text = text } } },
                outputDimensionality = 3072 // [v19.0] MariaDB 11.8 고도화를 위해 3072차원으로 상향
            };
 
            var ApiUrl = string.Format(EmbeddingUrlTemplate, ModelName, _apiKey);
            var Response = await httpClient.PostAsJsonAsync(ApiUrl, RequestBody);
 
            if (!Response.IsSuccessStatusCode)
            {
                var ErrorMsg = await Response.Content.ReadAsStringAsync();
                logger.LogError("[IAMF 오류] Gemini Embedding 호출 실패: {StatusCode}, {ErrorMsg}", Response.StatusCode, ErrorMsg);
                return Array.Empty<float>();
            }
 
            var JsonDoc = await Response.Content.ReadFromJsonAsync<JsonDocument>();
            var Values = JsonDoc?.RootElement
                .GetProperty("embedding")
                .GetProperty("values");
 
            if (Values == null) return Array.Empty<float>();
 
            var Result = new float[Values.Value.GetArrayLength()];
            int I = 0;
            foreach (var Val in Values.Value.EnumerateArray())
            {
                Result[I++] = Val.GetSingle();
            }
 
            return Result;
        }
        catch (Exception Ex)
        {
            logger.LogError(Ex, "[IAMF 오류] Gemini Embedding 연동 중 예외 발생");
            return Array.Empty<float>();
        }
    }
}
