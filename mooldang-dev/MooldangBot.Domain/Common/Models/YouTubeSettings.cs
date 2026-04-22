namespace MooldangBot.Domain.Common.Models;

/// <summary>
/// [v13.0] 유튜브 서비스 연동을 위한 설정 모델 (YouTube Recon Synergy)
/// </summary>
public class YouTubeSettings
{
    /// <summary>
    /// Google Cloud Console에서 발급받은 정식 API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// [v13.1] API 할당량 소진 시 자동으로 YoutubeExplode를 사용할지 여부
    /// </summary>
    public bool EnableFallback { get; set; } = true;
}
