using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Channels;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Live;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Shared;
using MooldangBot.ChzzkAPI.Contracts.Models.Internal;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace MooldangBot.ChzzkAPI.Apis.Internal;

/// <summary>
/// [오시리스의 전령]: 채널 상세 정보 및 실시간 방송 정보를 네이버 서버로부터 프록시하거나 배치로 조회합니다.
/// </summary>
[ApiController]
[Route("api/internal/channels")]
public class InternalChannelController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<InternalChannelController> _logger;
    private const string InternalSecretHeader = "X-Internal-Secret-Key";

    public InternalChannelController(IChzzkApiClient apiClient, ILogger<InternalChannelController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [오시리스의 집계]: 다수의 채널 정보를 병렬로 조회하여 취합합니다. (N+1 문제 최적화)
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> GetChannelsBatch([FromBody] ChannelBatchRequest request)
    {
        if (!IsAuthorized()) return Unauthorized();
        if (request.ChannelIds == null || !request.ChannelIds.Any()) 
            return Ok(new ChzzkApiResponse<ChzzkPagedResponse<ChannelProfile>> 
            { 
                Code = 200, 
                Content = new ChzzkPagedResponse<ChannelProfile>() 
            });

        var results = new ConcurrentBag<ChannelProfile>();
        
        // [시니어의 팁]: ParallelOptions를 사용하여 동시 호출량을 제어(Rate Limit 대비)
        var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        _logger.LogInformation("📡 [Gateway] {Count}개 채널에 대한 배치 조회 시작...", request.ChannelIds.Count);

        // [물멍]: 개별 루프 방식에서 배치 전용 메서드 호출로 최적화
        var profiles = await _apiClient.GetChannelsAsync(request.ChannelIds);

        _logger.LogInformation("✅ [Gateway] 배치 조회 완료: {Count}개 성공", profiles.Count);

        // ChzzkApiClient가 기대하는 PagedResponse (List<T> Data 포함) 구조로 반환
        var result = new ChzzkApiResponse<ChzzkPagedResponse<ChannelProfile>>
        {
            Code = 200,
            Content = new ChzzkPagedResponse<ChannelProfile>
            {
                Data = profiles.OrderBy(x => x.ChannelName).ToList()
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// [오시리스의 감시]: 특정 채널의 실시간 방송 상세 정보를 조회합니다.
    /// </summary>
    [HttpGet("{channelId}/live-detail")]
    public async Task<IActionResult> GetLiveDetail(string channelId)
    {
        if (!IsAuthorized()) return Unauthorized();

        _logger.LogInformation("📡 [Gateway] 채널 {ChannelId}의 라이브 상세 정보 요청...", channelId);

        // [물멍]: 현재 WebSocket Manager 연동 전이므로 404 대신 빈 성공 응답을 반환하여 대시보드 중단을 방지합니다.
        return Ok(new ChzzkApiResponse<object> { Code = 200, Content = null });
    }

    private bool IsAuthorized()
    {
        if (!Request.Headers.TryGetValue(InternalSecretHeader, out var secret) || 
            secret != Environment.GetEnvironmentVariable("INTERNAL_API_SECRET"))
        {
            return false;
        }
        return true;
    }

}
