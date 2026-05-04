using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    public class LogRouletteResults
    {
        [Key]
        public long Id { get; set; }

        public int StreamerProfileId { get; set; } // [v4.4] ChzzkUid 제거 및 FK 도입

        public int RouletteId { get; set; }

        public int? RouletteItemId { get; set; } // [v4.4] 아이템 추적용 FK 추가 (SetNull 대응)

        [MaxLength(100)]
        public string RouletteName { get; set; } = string.Empty;

        public int GlobalViewerId { get; set; } // [v4.4] ViewerUid/Hash 제거 및 FK 도입

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public bool IsMission { get; set; }

        public RouletteLogStatus Status { get; set; } = RouletteLogStatus.Pending;

        public KstClock CreatedAt { get; set; } = KstClock.Now;

        public KstClock? ProcessedAt { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

        [JsonIgnore]
        public CoreGlobalViewers? CoreGlobalViewers { get; set; }

        [JsonIgnore]
        public FuncRouletteItems? FuncRouletteItems { get; set; }
    }
}
