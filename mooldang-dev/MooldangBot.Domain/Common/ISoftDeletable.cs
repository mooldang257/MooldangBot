using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Common;

/// <summary>
/// [존재의 보전]: 논리적 삭제를 가능하게 하는 인터페이스입니다.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    KstClock? DeletedAt { get; set; }
}
