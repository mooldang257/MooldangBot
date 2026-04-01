using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
[Table("songlistsessions")]
[Index(nameof(StreamerProfileId), nameof(IsActive))]
public class SonglistSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

        public KstClock StartedAt { get; set; }
        
        public KstClock? EndedAt { get; set; }

        public int RequestCount { get; set; } = 0;

        public int CompleteCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
