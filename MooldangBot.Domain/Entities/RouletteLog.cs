using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

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

        // [v4.0] 수호자의 암호: 개인정보 보호를 위한 식별자 암호화 및 해싱
        public string? ViewerUid { get; set; }
        
        [MaxLength(64)]
        public string? ViewerUidHash { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public bool IsMission { get; set; }

        public RouletteLogStatus Status { get; set; } = RouletteLogStatus.Pending;

        public KstClock CreatedAt { get; set; } = KstClock.Now;

        public KstClock? ProcessedAt { get; set; }
    }
}
