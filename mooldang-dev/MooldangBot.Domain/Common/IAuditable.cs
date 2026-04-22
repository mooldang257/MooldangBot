using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Common;

/// <summary>
/// [서기의 기록]: 엔터티의 생성 및 수정 시간을 추적하는 인터페이스입니다.
/// </summary>
public interface IAuditable
{
    KstClock CreatedAt { get; set; }
    KstClock? UpdatedAt { get; set; }
}
