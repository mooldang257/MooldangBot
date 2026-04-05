using Microsoft.AspNetCore.Mvc;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.SongRequest;

/// <summary>
/// 표준 통신 프로토콜 (Result<T>) 적용 검증을 위한 샘플 컨트롤러
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SongRequestController : ControllerBase
{
    [HttpGet("pending/{chzzkUid}")]
    public IActionResult GetPendingSongs(string chzzkUid)
    {
        // [물멍]: DB 없이 즉시 결과를 확인할 수 있는 Mock 데이터
        var mockSongs = new List<object>
        {
            new { Id = 1, Title = "Night Glow", Artist = "HoYoMiX", Requester = "물댕댕", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new { Id = 2, Title = "Bokurano", Artist = "Eve", Requester = "가나다라", CreatedAt = DateTime.UtcNow.AddMinutes(-2) }
        };

        // 완전히 규격화된 Result<T> 봉투에 담아 반환
        return Ok(Result<List<object>>.Success(mockSongs));
    }
}
