using MooldangBot.Domain.Contracts.AI.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.ApiClients.Philosophy;

/// <summary>
/// [신속의 추론기]: Groq API를 통해 초광속으로 텍스트 응답을 생성하는 구현체입니다.
/// Llama 3 8B 모델을 사용하여 지연시간을 최소화합니다.
/// </summary>
public class GroqLlmService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<GroqLlmService> logger) : ILlmService
{
    private readonly string _apiKey = config["GROQ_API_KEY"] ?? string.Empty;
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string ModelName = "llama-3.1-8b-instant";

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("[Groq] API 키가 설정되지 않았습니다.");
            return string.Empty;
        }

        try
        {
            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.0,
                max_tokens = 300
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                logger.LogError("[Groq] API 호출 실패: {StatusCode}, {ErrorMsg}", response.StatusCode, errorMsg);
                return string.Empty;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            
            var aiText = jsonDoc?.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            logger.LogDebug("[Groq] Raw Response Content: {Content}", aiText);
            return aiText?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Groq] 연동 중 예외 발생");
            return string.Empty;
        }
    }

    /// <summary>
    /// [주의] Groq는 현재 임베딩을 직접 지원하지 않을 수 있습니다. 
    /// 필요 시 기존의 로컬 임베딩 서비스나 Gemini를 병행 사용해야 합니다.
    /// </summary>
    public Task<float[]> GetEmbeddingAsync(string text)
    {
        // Groq는 채팅 전용이므로 임베딩 요청 시 빈 배열 반환 또는 예외 처리
        logger.LogWarning("[Groq] Embedding 기능은 지원되지 않습니다.");
        return Task.FromResult(Array.Empty<float>());
    }
}
