using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId), nameof(Id))]
    [Index(nameof(StreamerProfileId), nameof(Status), nameof(CreatedAt))]
    public class SongQueue : ISoftDeletable, IAuditable
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

        /// <summary>
        /// [v6.2.2] 노래책 연동 (선택 사항: 노래책에 있는 곡인 경우 연결)
        /// </summary>
        public int? SongBookId { get; set; }

        [ForeignKey(nameof(SongBookId))]
        public virtual SongBook? SongBook { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // 곡 제목

        [MaxLength(100)]
        public string? Artist { get; set; } // 가수

        [Required]
        public SongStatus Status { get; set; } = SongStatus.Pending; // [v6.2.2] Enum 전환

        /// <summary>
        /// [v13.1] Snowflake 알고리즘 기반의 전역 유일 식별자입니다.
        /// </summary>
        public long SongLibraryId { get; set; }

        public int SortOrder { get; set; } = 0; // 드래그 앤 드롭 정렬을 기억하기 위한 순서 번호

        // [v6.2.2] 거버넌스 및 감사 필드 통합
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public KstClock? DeletedAt { get; set; }

        public string? RequesterNickname { get; set; } // [v14.2] 신청 시점 닉네임 (스냅샷)
        public int? Cost { get; set; } // [v14.1] 후원 금액
        public CommandCostType? CostType { get; set; } // [v14.1] 후원 수단 (치즈/포인트)

        public KstClock CreatedAt { get; set; } = KstClock.Now; 
        public KstClock? UpdatedAt { get; set; }
    }
}