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
    private const string GenerateUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={0}";
    private const string EmbeddingUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={0}";

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("[IAMF 경고] Gemini API 키가 설정되지 않았습니다. 기본 답변으로 대체합니다.");
            return "[지혜의 부재] 현재 신경망 연결이 끊겨 있어 깊은 대화가 어렵습니다.";
        }

        try
        {
            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = userMessage } } }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 300
                }
            };

            var apiUrl = string.Format(GenerateUrlTemplate, _apiKey);
            var response = await httpClient.PostAsJsonAsync(apiUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                logger.LogError("[IAMF 오류] Gemini API 호출 실패: {StatusCode}, {ErrorMsg}", response.StatusCode, errorMsg);
                return string.Empty;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var aiText = jsonDoc?.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return aiText?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IAMF 오류] Gemini 연동 중 예외 발생");
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
            var requestBody = new
            {
                model = "models/text-embedding-004",
                content = new { parts = new[] { new { text = text } } }
            };

            var apiUrl = string.Format(EmbeddingUrlTemplate, _apiKey);
            var response = await httpClient.PostAsJsonAsync(apiUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                logger.LogError("[IAMF 오류] Gemini Embedding 호출 실패: {StatusCode}, {ErrorMsg}", response.StatusCode, errorMsg);
                return Array.Empty<float>();
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var values = jsonDoc?.RootElement
                .GetProperty("embedding")
                .GetProperty("values");

            if (values == null) return Array.Empty<float>();

            var result = new float[values.Value.GetArrayLength()];
            int i = 0;
            foreach (var val in values.Value.EnumerateArray())
            {
                result[i++] = val.GetSingle();
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IAMF 오류] Gemini Embedding 연동 중 예외 발생");
            return Array.Empty<float>();
        }
    }
}
