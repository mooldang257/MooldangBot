using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Table("songlistsessions")]
    [Index(nameof(ChzzkUid), nameof(IsActive))]
    public class SonglistSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        public KstClock StartedAt { get; set; }
        
        public KstClock? EndedAt { get; set; }

        public int RequestCount { get; set; } = 0;

        public int CompleteCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
