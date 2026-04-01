using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerChzzkUid), nameof(Points))]
    public class ViewerProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string StreamerChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ViewerUid { get; set; } = string.Empty;

        /// <summary>
        /// [v4.0] 수호자의 각인: 암호화된 ViewerUid 검색을 위한 SHA-256 해시 필드입니다.
        /// </summary>
        [MaxLength(64)]
        public string ViewerUidHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Nickname { get; set; } = string.Empty;

        [ConcurrencyCheck]
        public int Points { get; set; } = 0;

        [ConcurrencyCheck]
        public int AttendanceCount { get; set; } = 0;

        public int ConsecutiveAttendanceCount { get; set; } = 0;

        public KstClock? LastAttendanceAt { get; set; }
    }
}
