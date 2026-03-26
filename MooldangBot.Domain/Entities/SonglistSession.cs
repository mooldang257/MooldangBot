using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MooldangBot.Domain.Entities
{
    [Table("songlistsessions")]
    public class SonglistSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }
        
        public DateTime? EndedAt { get; set; }

        public int RequestCount { get; set; } = 0;

        public int CompleteCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
