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
/// [거울의 신경망]: Google Gemini API를 통해 실제 인공지능 응답을 생성하는 실전 구현체입니다.
/// </summary>
public class GeminiLlmService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<GeminiLlmService> logger) : ILlmService
{
    // [거울의 신경망]: .env의 GEMINI_KEY 또는 appsettings.json의 Gemini:ApiKey 사용
    private readonly string _apiKey = config["GEMINI_KEY"] ?? config["Gemini:ApiKey"] ?? string.Empty;
    private const string ApiUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={0}";

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        // 1. [지혜의 발현]: API 키 미설정 시 방어 로직
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("[IAMF 경고] Gemini API 키가 설정되지 않았습니다. 기본 답변으로 대체합니다.");
            return "[지혜의 부재] 현재 신경망 연결이 끊겨 있어 깊은 대화가 어렵습니다.";
        }

        try
        {
            // 2. [페이로드 구성]: 시스템 프롬프트 및 메시지 매핑
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
                    maxOutputTokens = 200 // 방송 채팅의 간결함을 위해 제한
                }
            };

            // 3. [API 호출]: 비동기 요청 전송
            var apiUrl = string.Format(ApiUrlTemplate, _apiKey);
            var response = await httpClient.PostAsJsonAsync(apiUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                logger.LogError($"[IAMF 오류] Gemini API 호출 실패: {response.StatusCode}, {errorMsg}");
                return string.Empty; // 발화하지 않음 (거울의 침묵)
            }

            // 4. [응답 파싱]: JSON에서 텍스트 결과만 추출
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
}
