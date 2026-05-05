using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId))]
    public class SysOverlayPresets
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = false;

        // 프리셋 설정 데이터를 JSON으로 저장
        public string ConfigJson { get; set; } = "{}";

        public KstClock CreatedAt { get; set; } = KstClock.Now;
        public KstClock UpdatedAt { get; set; } = KstClock.Now;
    }
}
