using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Modules.SongBookModule.Features.Commands;
using MooldangBot.Modules.SongBookModule.Features.Queries;
using MooldangBot.Domain.Common;

namespace MooldangBot.Modules.SongBookModule.Controllers;

/// <summary>
/// [오시리스의 서고]: 송북(노래 신청 및 오마카세) 관련 기능을 담당하는 모듈 컨트롤러입니다.
/// (v15.1: 핀 테크니컬 아키텍처에 따라 모든 비즈니스 로직을 MediatR 핸들러로 위임합니다.)
/// </summary>
[ApiController]
[Route("api/v1/songbook")]
[Authorize]
public class SongBookController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// [오마카세 목록 조회]: 스트리머의 활성화된 오마카세 리스트를 반환합니다.
    /// </summary>
    [HttpGet("omakase/{chzzkUid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<object>>> GetOmakaseList(string chzzkUid, [FromQuery] int? lastId = null)
        => Ok(await mediator.Send(new GetOmakaseListQuery(chzzkUid, LastId: lastId)));

    /// <summary>
    /// [송리스트 데이터 조회]: 현재 대기열과 오마카세 설정을 통합 조회합니다.
    /// </summary>
    [HttpGet("list/{chzzkUid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<object>>> GetSonglistData(string chzzkUid)
        => Ok(await mediator.Send(new GetSonglistDataQuery(chzzkUid)));

    /// <summary>
    /// [송리스트 활성화 상태 조회]: 현재 노래 신청 세션이 열려있는지 확인합니다.
    /// </summary>
    [HttpGet("status/{chzzkUid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<object>>> GetSonglistStatus(string chzzkUid)
        => Ok(await mediator.Send(new GetSonglistStatusQuery(chzzkUid)));

    /// <summary>
    /// [송리스트 상태 토글]: 노래 신청 세션을 열거나 닫습니다. (스트리머 전용)
    /// </summary>
    [HttpPost("status/toggle")]
    public async Task<ActionResult<Result<object>>> ToggleStatus([FromBody] ToggleSonglistStatusCommand command)
        => Ok(await mediator.Send(command));

    /// <summary>
    /// [오마카세 카운트 업데이트]: 특정 오마카세 메뉴의 수량을 변경합니다.
    /// </summary>
    [HttpPost("omakase/count")]
    public async Task<ActionResult<Result<object>>> UpdateOmakaseCount([FromBody] UpdateOmakaseCountCommand command)
        => Ok(await mediator.Send(command));

    /// <summary>
    /// [시뮬레이션 채팅]: 테스트용 채팅 이벤트를 발생시킵니다.
    /// </summary>
    [HttpPost("simulator/chat")]
    public async Task<ActionResult<Result<object>>> SimulatorChat([FromBody] SimulatorChatCommand command)
        => Ok(await mediator.Send(command));
}
