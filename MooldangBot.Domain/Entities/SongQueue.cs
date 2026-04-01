using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId), nameof(Id))]
    [Index(nameof(StreamerProfileId), nameof(Status), nameof(CreatedAt))]
    public class SongQueue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual StreamerProfile? StreamerProfile { get; set; }

        // [v4.5 확장] 시청자 추적을 위한 글로벌 시청자 ID
        public int? GlobalViewerId { get; set; }

        [ForeignKey(nameof(GlobalViewerId))]
        public virtual GlobalViewer? GlobalViewer { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // 곡 제목

        [MaxLength(100)]
        public string? Artist { get; set; } // 가수

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // 상태: Pending(대기), Playing(재생중), Completed(완료)

        public int SortOrder { get; set; } = 0; // 드래그 앤 드롭 정렬을 기억하기 위한 순서 번호

        public KstClock CreatedAt { get; set; } = KstClock.Now; // 신청된 시간 (KST)
    }
}