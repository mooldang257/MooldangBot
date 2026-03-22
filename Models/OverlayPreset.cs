using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MooldangAPI.Models
{
    public class OverlayPreset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // 프리셋 설정 데이터를 JSON으로 저장
        public string ConfigJson { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
