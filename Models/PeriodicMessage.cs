using System;
using System.ComponentModel.DataAnnotations;

namespace MooldangAPI.Models
{
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
