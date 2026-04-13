using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [파로스의 통합]: 명령어의 생성, 수정, 삭제 및 연관 데이터의 라이프사이클을 통괄 관리하는 서비스입니다.
/// </summary>
public interface IUnifiedCommandService
{
    /// <summary>
    /// 명령어를 저장하거나 수정합니다. (Upsert)
    /// </summary>
    Task<UnifiedCommand> UpsertCommandAsync(string chzzkUid, SaveUnifiedCommandRequest req);

    /// <summary>
    /// 명령어를 삭제하고 연관된 데이터(오마카세 아이템 등)를 함께 정리합니다.
    /// </summary>
    Task DeleteCommandAsync(string chzzkUid, int id);

    /// <summary>
    /// 명령어의 활성화 상태(ON/OFF)를 토글합니다.
    /// </summary>
    Task ToggleCommandAsync(string chzzkUid, int id);

    /// <summary>
    /// [파로스의 시작]: 신규 스트리머 가입 시 필요한 기본 명령어 세트(신청, 룰렛, 관리 명령어 등)를 자동 생성합니다.
    /// </summary>
    Task InitializeDefaultCommandsAsync(string chzzkUid);
}
