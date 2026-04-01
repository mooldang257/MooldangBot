using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;
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
        public KstClock? LastSentAt { get; set; }
    }
}
