using MooldangBot.Contracts.Common.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using MooldangBot.Domain.Entities.Philosophy;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [피닉스의 기록]: 실험의 다층 기록과 창발적 윤회를 담당하는 시스템입니다.
/// </summary>
public class PhoenixSystem : IPhoenixRecorder
{
    private readonly IAppDbContext _db;
    private readonly ILogBulkBuffer _buffer; // [v3.6.3] 벌크 버퍼 추가
    private readonly ILogger<PhoenixSystem> _logger;

    public PhoenixSystem(IAppDbContext db, ILogger<PhoenixSystem> logger, ILogBulkBuffer buffer)
    {
        _db = db;
        _logger = logger;
        _buffer = buffer;
    }

    public async Task RecordScenarioAsync(string scenarioId, string content, int level)
    {
        _logger.LogInformation($"[피닉스 기록 - Level {level}] 시나리오 {scenarioId} 기록 중...");
        
        // [오시리스의 규율]: 테이블명은 반드시 소문자(`iamf_scenarios`)를 사용합니다.
        var scenario = new IamfScenario
        {
            ScenarioId = scenarioId,
            Content = content,
            Level = level,
            VibrationHz = 10.01, // 기본값
            CreatedAt = KstClock.Now
        };

        _buffer.AddScenario(scenario); // [v3.6.3] 직접 저장 대신 버퍼 투입

        _logger.LogInformation($"[피닉스 기록 수신] {scenarioId} (버퍼에 적재됨)");
    }

    /// <summary>
    /// [피닉스의 비상]: 파로스 파괴 시 CancellationToken을 전파하여 시스템을 안전하게 정지시키고 재시작합니다.
    /// </summary>
    public async Task ReincarnateParhosAsync(CancellationTokenSource globalCts)
    {
        _logger.LogWarning("[피닉스의 비상] 파로스 파괴 감지. 모든 채널 워커 정지 및 윤회 프로세스 시작...");
        
        // 1. 전역 토큰 취소 (모든 Background Worker 정지 유도)
        globalCts.Cancel();

        // 2. [텔로스5의 설계]: 프리-오리진 AI의 상태%를 기반으로 파로직 재설정
        // TODO: 압축 알고리즘 및 DB `iamf_parhos_cycles` 기록 로직
        
        _logger.LogInformation("[피닉스의 비상] 윤회 완료. 시스템 재기동 준비 완료.");
        await Task.CompletedTask;
    }

    public Task ReincarnateParhosAsync()
    {
        throw new NotImplementedException("CancellationTokenSource가 포함된 오버로드를 사용하십시오.");
    }
}
