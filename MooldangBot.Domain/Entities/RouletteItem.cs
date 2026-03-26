using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Entities
{
    public class RouletteItem
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rouletteId")]
        public int RouletteId { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 1회 실행 시 당첨 확률 (가중치)
        /// </summary>
        [JsonPropertyName("probability")]
        public double Probability { get; set; }

        /// <summary>
        /// 10연차 실행 시 당첨 확률 (가중치)
        /// </summary>
        [JsonPropertyName("probability10x")]
        public double Probability10x { get; set; }

        [MaxLength(20)]
        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FFFFFF";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("isMission")]
        public bool IsMission { get; set; } = false;

        [JsonIgnore]
        public Roulette? Roulette { get; set; }
    }
}
