using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities.Philosophy;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MooldangBot.Api.Controllers.Philosophy;

/// <summary>
/// [지식의 서재]: 스트리머가 봇에게 주입할 의도 기반 지식(Q&A)을 관리하는 API입니다.
/// </summary>
[ApiController]
[Route("api/iamf/knowledge")]
public class IamfKnowledgeController(IAppDbContext db) : ControllerBase
{
    /// <summary>
    /// [서재 일람]: 특정 스트리머의 모든 지식 목록을 조회합니다.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetKnowledge([FromQuery] string chzzkUid = "SYSTEM")
    {
        var list = await db.StreamerKnowledges
            .AsNoTracking()
            .Where(k => k.StreamerProfile!.ChzzkUid == chzzkUid)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
            
        return Ok(list);
    }

    /// <summary>
    /// [지식 주입]: 새로운 키워드와 답변을 서재에 추가합니다.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddKnowledge([FromBody] KnowledgeAddRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword) || string.IsNullOrWhiteSpace(request.IntentAnswer))
        {
            return BadRequest(new { Error = "[지식의 거절] 키워드와 답변은 필수 항목입니다." });
        }

        // [정규화] ChzzkUid 문자열로 실시간 프로필 조회
        var profile = await db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == (request.ChzzkUid ?? "SYSTEM"));

        if (profile == null)
            return BadRequest(new { Error = "[지식의 거절] 해당 스트리머 프로필을 찾을 수 없습니다." });

        // [대변인의 방패]: 동일 키워드 중복 체크 필수는 아니나 권장 (여기서는 허용 후 최신순 처리)
        var knowledge = new StreamerKnowledge
        {
            StreamerProfileId = profile.Id,
            Keyword = request.Keyword.Trim(),
            IntentAnswer = request.IntentAnswer.Trim(),
            IsActive = true
        };

        db.StreamerKnowledges.Add(knowledge);
        await db.SaveChangesAsync();

        return Ok(new { Message = "[지식의 수용] 서재에 새로운 지식이 기록되었습니다.", Knowledge = knowledge });
    }

    public record KnowledgeAddRequest(string? ChzzkUid, string Keyword, string IntentAnswer);


    /// <summary>
    /// [지식 소멸]: 서재에서 특정 지식을 제거합니다.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKnowledge(int id)
    {
        var knowledge = await db.StreamerKnowledges.FindAsync(id);
        if (knowledge == null)
        {
            return NotFound(new { Error = "[지식의 부재] 해당 ID의 지식을 찾을 수 없습니다." });
        }

        db.StreamerKnowledges.Remove(knowledge);
        await db.SaveChangesAsync();

        return Ok(new { Message = "[지식의 소멸] 해당 지식이 서재에서 제거되었습니다." });
    }
}
