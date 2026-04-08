using System;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 가드]: 분산 환경에서 동일한 메시지가 중복 처리되는 것을 방지하기 위한 멱등성 관리 서비스입니다.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// [체크포인트]: 특정 요청 ID가 이미 처리 중이거나 완료되었는지 확인하고, 아닐 경우 고정 시간 동안 잠금을 설정합니다.
    /// </summary>
    /// <param name="key">멱등성 보장을 위한 고유 키 (예: MessageId, CorrelationId)</param>
    /// <param name="expiry">멱등성 유지 기간</param>
    /// <returns>최초 처리 시도인 경우 true, 중복 요청인 경우 false</returns>
    Task<bool> TryAcquireAsync(string key, TimeSpan expiry);

    /// <summary>
    /// [영구 기록]: 처리가 완료되었음을 명시적으로 기록합니다 (필요 시).
    /// </summary>
    Task MarkAsCompletedAsync(string key, TimeSpan expiry);
}
