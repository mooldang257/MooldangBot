using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid))]
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

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("items")]
        public List<RouletteItem> Items { get; set; } = new();
    }
}
