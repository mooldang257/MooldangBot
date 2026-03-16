using System.ComponentModel.DataAnnotations;

namespace MooldangAPI.Models;

public class StreamerCommand
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChzzkUid { get; set; } = string.Empty; // 이 명령어의 주인 (스트리머)

    [Required]
    [MaxLength(50)]
    public string CommandKeyword { get; set; } = string.Empty; // 발동 키워드 (예: "!노래책", "!유튜브")

    [Required]
    [MaxLength(20)]
    public string ActionType { get; set; } = "Notice"; // 행동 유형: "Notice"(상단공지), "Reply"(채팅답변) 등 확장 가능

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty; // 실행할 내용 (공지할 주소나 답변 텍스트)

    [Required]
    [MaxLength(20)]
    public string RequiredRole { get; set; } = "manager"; // 필요 권한: "streamer", "manager", "all"
}