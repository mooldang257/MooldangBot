using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid))]
    public class StreamerOmakaseItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Icon { get; set; } = "🍣";

        [ConcurrencyCheck]
        public int Count { get; set; } = 0;

    }
}
