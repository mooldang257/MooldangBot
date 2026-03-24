using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MooldangAPI.Models
{
    public class RouletteItem
    {
        [Key]
        public int Id { get; set; }

        public int RouletteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 1회 실행 시 당첨 확률 (가중치)
        /// </summary>
        public double Probability { get; set; }

        /// <summary>
        /// 10연차 실행 시 당첨 확률 (가중치)
        /// </summary>
        public double Probability10x { get; set; }

        [MaxLength(20)]
        public string Color { get; set; } = "#FFFFFF";

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public Roulette? Roulette { get; set; }
    }
}
