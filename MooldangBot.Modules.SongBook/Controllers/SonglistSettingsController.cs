using MediatR;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Modules.SongBookModule.Features.Commands;
using MooldangBot.Modules.SongBookModule.Features.Queries;

namespace MooldangBot.Modules.SongBookModule.Controllers;

/// <summary>
/// [오시리스의 설정]: 송북 디자인 및 관련 명령어 설정을 담당하는 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/v1/songlist-settings")]
public class SonglistSettingsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// [송리스트 설정 데이터 조회]: 스트리머 전용 송북 설정 데이터를 가져옵니다.
    /// </summary>
    [HttpGet("{chzzkUid}")]
    public async Task<ActionResult<Result<object>>> GetData(string chzzkUid)
        => Ok(await mediator.Send(new GetSonglistSettingsDataQuery(chzzkUid)));

    /// <summary>
    /// [라벨 업데이트]: 오버레이에 표시될 라벨 텍스트를 수정합니다.
    /// </summary>
    [HttpPost("labels")]
    public async Task<ActionResult<Result<object>>> UpdateLabels([FromBody] UpdateLabelsCommand command)
        => Ok(await mediator.Send(command));

    /// <summary>
    /// [송리스트 설정 업데이트]: 디자인 정보 및 연동 명령어를 일괄 동기화합니다.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Result<object>>> UpdateSettings([FromBody] UpdateSonglistSettingsCommand command)
        => Ok(await mediator.Send(command));
}
