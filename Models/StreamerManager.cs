using System.ComponentModel.DataAnnotations;

namespace MooldangAPI.Models;

public class StreamerManager
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StreamerChzzkUid { get; set; } = string.Empty; // 관리 대상 스트리머

    [Required]
    [MaxLength(50)]
    public string ManagerChzzkUid { get; set; } = string.Empty; // 권한을 가진 사용자

    [MaxLength(20)]
    public string Role { get; set; } = "manager"; // "manager", "admin" 등 확장 가능

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
