using System.ComponentModel.DataAnnotations;

namespace MooldangAPI.Models
{
    public class Roulette
    {
        [Key]
        public int Id { get; set; }

        public string? ChzzkUid { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public RouletteType Type { get; set; }

        [Required]
        [MaxLength(50)]
        public string Command { get; set; } = "!룰렛";

        public int CostPerSpin { get; set; } = 1000;

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<RouletteItem> Items { get; set; } = new();
    }
}
