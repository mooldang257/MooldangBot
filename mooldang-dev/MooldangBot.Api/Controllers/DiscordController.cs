using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;

namespace MooldangBot.Api.Controllers;

/// <summary>
/// [이지스의 파수꾼]: 디스코드 인터랙션(Slash Commands 등) 수신 및 보안 검증을 담당합니다.
/// </summary>
[ApiController]
[Route("api/discord")]
public class DiscordController(IConfiguration configuration, ILogger<DiscordController> logger) : ControllerBase
{
    private readonly string? _publicKey = configuration["Discord:PublicKey"];

    [HttpPost("interactions")]
    public async Task<IActionResult> HandleInteraction()
    {
        // 1. [보안 검증]: 디스코드 서명 확인 (Ed25519)
        if (!Request.Headers.TryGetValue("X-Signature-Ed25519", out var signature) ||
            !Request.Headers.TryGetValue("X-Signature-Timestamp", out var timestamp))
        {
            return Unauthorized("Missing signature headers");
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        if (!VerifySignature(body, signature!, timestamp!))
        {
            logger.LogWarning("⚠️ [Discord] 유효하지 않은 보안 서명이 감지되었습니다.");
            return Unauthorized("Invalid signature");
        }

        // 2. [인터랙션 처리]
        var interaction = JsonDocument.Parse(body);
        var type = interaction.RootElement.GetProperty("type").GetInt32();

        // Type 1: PING (연결 확인용)
        if (type == 1)
        {
            logger.LogInformation("🚀 [Discord] PING 수신 - PONG 응답으로 연결을 확인합니다.");
            return Ok(new { type = 1 });
        }

        // TODO: 기타 명령어 처리 로직 (Phase 2에서 확장 예정)
        logger.LogDebug("[Discord] 수신된 인터랙션 타입: {Type}", type);
        return Ok(new { type = 4, data = new { content = "명령어를 수신했습니다." } });
    }

    private bool VerifySignature(string body, string signature, string timestamp)
    {
        if (string.IsNullOrEmpty(_publicKey)) return false;

        try
        {
            var publicKeyBytes = Convert.FromHexString(_publicKey);
            var signatureBytes = Convert.FromHexString(signature);
            var messageBytes = Encoding.UTF8.GetBytes(timestamp + body);

            // NSec (Ed25519) 검증 로직
            var algorithm = SignatureAlgorithm.Ed25519;
            var publicKey = PublicKey.Import(algorithm, publicKeyBytes, KeyBlobFormat.RawPublicKey);
            
            return algorithm.Verify(publicKey, messageBytes, signatureBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [Discord] 서명 검증 중 오류 발생");
            return false;
        }
    }
}
