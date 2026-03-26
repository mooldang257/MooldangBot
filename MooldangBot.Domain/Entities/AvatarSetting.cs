using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Domain.Entities
{
    [Index(nameof(ChzzkUid), IsUnique = true)]
    public class AvatarSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
        
        public bool ShowNickname { get; set; } = true;
        
        public bool ShowChat { get; set; } = true;
        
        // 추가: 채팅 미입력 시 사라지는 시간 (초 단위). 0 이면 안 사라짐. 기본값 60초.
        public int DisappearTimeSeconds { get; set; } = 60;

        [MaxLength(1000)]
        public string? WalkingImageUrl { get; set; }

        [MaxLength(1000)]
        public string? StopImageUrl { get; set; }

        [MaxLength(1000)]
        public string? InteractionImageUrl { get; set; }
    }
}
