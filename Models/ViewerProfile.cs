using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MooldangAPI.Models
{
    [Index(nameof(StreamerChzzkUid), nameof(ViewerUid), IsUnique = true)]
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

        [MaxLength(100)]
        public string Nickname { get; set; } = string.Empty;

        [ConcurrencyCheck]
        public int Points { get; set; } = 0;

        [ConcurrencyCheck]
        public int AttendanceCount { get; set; } = 0;

        public int ConsecutiveAttendanceCount { get; set; } = 0;

        public DateTime? LastAttendanceAt { get; set; }
    }
}
