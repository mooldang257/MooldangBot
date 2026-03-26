using System.ComponentModel.DataAnnotations;

namespace MooldangBot.Domain.Entities
{
    public class StreamerOmakaseItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "물마카세";

        [Required]
        [MaxLength(50)]
        public string Command { get; set; } = "!물마카세";

        [Required]
        [MaxLength(20)]
        public string Icon { get; set; } = "🍣";

        public int Price { get; set; } = 1000;

        [ConcurrencyCheck]
        public int Count { get; set; } = 0;
    }
}
