using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId))] // [v4.4] 정문화된 인덱스
    public class Roulette : ISoftDeletable, IAuditable
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("streamerProfileId")]
        public int StreamerProfileId { get; set; } // [v4.4] ChzzkUid 제거 및 FK 도입

        [Required]
        [MaxLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화 (토글용)

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false; // [v6.1.5] 존재 거버넌스 (복구 가능)

        [JsonPropertyName("deletedAt")]
        public KstClock? DeletedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public KstClock CreatedAt { get; set; } = KstClock.Now;

        [JsonPropertyName("updatedAt")]
        public KstClock? UpdatedAt { get; set; }

        [JsonPropertyName("items")]
        public List<RouletteItem> Items { get; set; } = new();

        // [v4.4] 내비게이션 속성 추가
        [JsonIgnore]
        public StreamerProfile? StreamerProfile { get; set; }
    }
}
