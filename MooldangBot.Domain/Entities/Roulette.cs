using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Entities
{
    public class Roulette
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("chzzkUid")]
        public string? ChzzkUid { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("type")]
        public RouletteType Type { get; set; }

        [Required]
        [MaxLength(50)]
        [JsonPropertyName("command")]
        public string Command { get; set; } = "!룰렛";

        [JsonPropertyName("costPerSpin")]
        public int CostPerSpin { get; set; } = 1000;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("items")]
        public List<RouletteItem> Items { get; set; } = new();
    }
}
