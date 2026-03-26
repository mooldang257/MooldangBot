using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid), nameof(Id))]
    public class SongBook
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Artist { get; set; }

        public bool IsActive { get; set; } = true;

        public int UsageCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
