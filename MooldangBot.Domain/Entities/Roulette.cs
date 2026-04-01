using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId))] // [v4.4] 정문화된 인덱스
    public class Roulette
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

        [JsonPropertyName("updatedAt")]
        public KstClock UpdatedAt { get; set; } = KstClock.Now;

        [JsonPropertyName("items")]
        public List<RouletteItem> Items { get; set; } = new();

        // [v4.4] 내비게이션 속성 추가
        [JsonIgnore]
        public StreamerProfile? StreamerProfile { get; set; }
    }
}
