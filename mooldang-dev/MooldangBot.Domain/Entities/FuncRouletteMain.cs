using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId))] // [v4.4] 정문화된 인덱스
    public class FuncRouletteMain : ISoftDeletable, IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int StreamerProfileId { get; set; } // [v4.4] ChzzkUid 제거 및 FK 도입

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true; // [v6.1.5] 기능 활성화 (토글용)

        public bool IsDeleted { get; set; } = false; // [v6.1.5] 존재 거버넌스 (복구 가능)

        public KstClock? DeletedAt { get; set; }

        public KstClock CreatedAt { get; set; } = KstClock.Now;

        public KstClock? UpdatedAt { get; set; }

        public List<FuncRouletteItems> Items { get; set; } = new();

        // [v4.4] 내비게이션 속성 추가
        [JsonIgnore]
        public CoreStreamerProfiles? CoreStreamerProfiles { get; set; }
    }
}
