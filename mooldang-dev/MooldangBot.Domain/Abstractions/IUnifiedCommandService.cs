using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [파로스의 통합]: 명령어의 생성, 수정, 삭제 및 연관 데이터의 라이프사이클을 통괄 관리하는 서비스 인터페이스입니다.
/// (v15.2): 순환 참조 방지를 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public interface IUnifiedCommandService
{
    Task<UnifiedCommand> UpsertCommandAsync(string chzzkUid, SaveUnifiedCommandRequest req);
    Task DeleteCommandAsync(string chzzkUid, int id);
    Task ToggleCommandAsync(string chzzkUid, int id);
    Task InitializeDefaultCommandsAsync(string chzzkUid);
}
