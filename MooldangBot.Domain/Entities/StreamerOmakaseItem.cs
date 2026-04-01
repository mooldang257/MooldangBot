using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    // [v4.7 정규화] ChzzkUid -> StreamerProfileId
    [Index(nameof(StreamerProfileId))]
    public class StreamerOmakaseItem
    {
        [Key]
        public int Id { get; set; }

        // [v4.7 정규화] ChzzkUid -> StreamerProfileId
        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual StreamerProfile? StreamerProfile { get; set; }

        [Required]
        [MaxLength(20)]
        public string Icon { get; set; } = "🍣";

        [ConcurrencyCheck]
        public int Count { get; set; } = 0;
    }
}
