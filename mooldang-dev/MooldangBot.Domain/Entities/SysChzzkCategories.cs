using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities
{
    public class SysChzzkCategories
    {
        [Key]
        [MaxLength(100)]
        public string CategoryId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CategoryValue { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CategoryType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? PosterImageUrl { get; set; }

        public KstClock UpdatedAt { get; set; } = KstClock.Now;

        // 약어 목록 (Navigation Property)
        public ICollection<SysChzzkCategoryAliases> Aliases { get; set; } = new List<SysChzzkCategoryAliases>();
    }
}
