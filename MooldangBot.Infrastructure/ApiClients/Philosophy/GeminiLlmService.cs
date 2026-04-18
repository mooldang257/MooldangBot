using MooldangBot.Domain.Contracts.AI.Interfaces;
// ⚠️ [중요 - 삭제 금지]: 이 파일은 현재 비활성화 상태(DependencyInjection 참조)이나,
// 프로젝트의 핵심 자산(IAMF) 보존 및 향후 재활성화를 위해 절대로 삭제하지 마십시오.
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
    private const string ApiUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={0}";

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
                
                // [v2.0.0] 503 유연한 대응: 외부 신경망 과부하 시 침묵 대신 상황 공유
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    logger.LogWarning("⚠️ [오시리스의 경고] Gemini API 신경망 과부하 (503 Service Unavailable). 일시적인 응답 지연 상태입니다. 상세: {ErrorMsg}", errorMsg);
                    return "현재 물댕봇의 신경망이 과부하 상태입니다. 잠시 후 다시 시도해주세요. 💦";
                }

                // [v2.0.1] 429 대응: 무료 티어 할당량 초과 시 대응
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    logger.LogWarning("⚠️ [오시리스의 한계] Gemini API 할당량 초과 (429 Too Many Requests). 상세: {ErrorMsg}", errorMsg);
                    return "현재 AI 신경망의 무료 할당량이 모두 소진되었습니다. 약 1분 후 다시 시도해주세요. ⏳";
                }

                logger.LogError("[IAMF 오류] Gemini API 호출 실패: {StatusCode}, {ErrorMsg}", response.StatusCode, errorMsg);
                return string.Empty; // 기타 오류는 발화하지 않음 (거울의 침묵)
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
