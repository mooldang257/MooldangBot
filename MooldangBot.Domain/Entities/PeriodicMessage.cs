using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid))]
    public class PeriodicMessage
    {
        public int Id { get; set; }
        public string ChzzkUid { get; set; } = "";
        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastSentAt { get; set; }
    }
}
