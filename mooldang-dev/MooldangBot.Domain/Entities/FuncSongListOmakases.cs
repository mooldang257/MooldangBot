using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    // [v4.7 정규화] ChzzkUid -> StreamerProfileId
    [Index(nameof(StreamerProfileId))]
    public class FuncSongListOmakases
    {
        [Key]
        public int Id { get; set; }

        // [v4.7 정규화] ChzzkUid -> StreamerProfileId
        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        [Required]
        [MaxLength(500)]
        public string Icon { get; set; } = "🍣";

        [ConcurrencyCheck]
        public int Count { get; set; } = 0;

        public bool IsActive { get; set; } = true; // [v6.1.6] 오마카세 메뉴 활성화 (토글용)
    }
}
