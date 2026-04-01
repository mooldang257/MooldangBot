using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid))]
    public class SharedComponent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // Chat, Alert, Goal 등

        // 컴포넌트 설정 데이터를 JSON으로 저장
        public string ConfigJson { get; set; } = "{}";

        public KstClock CreatedAt { get; set; } = KstClock.Now;
    }
}
