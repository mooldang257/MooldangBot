using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Entities
{
    public class RouletteLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChzzkUid { get; set; } = string.Empty;

        public int RouletteId { get; set; }

        [MaxLength(100)]
        public string RouletteName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ViewerNickname { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public bool IsMission { get; set; }

        public RouletteLogStatus Status { get; set; } = RouletteLogStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
