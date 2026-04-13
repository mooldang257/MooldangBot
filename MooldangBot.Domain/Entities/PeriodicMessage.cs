using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(StreamerProfileId))]
    public class PeriodicMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StreamerProfileId { get; set; }

        [ForeignKey(nameof(StreamerProfileId))]
        public virtual StreamerProfile? StreamerProfile { get; set; }
        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public KstClock? LastSentAt { get; set; }
    }
}
