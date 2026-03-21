using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MooldangAPI.Models
{
    public class ChzzkCategory
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

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 약어 목록 (Navigation Property)
        public ICollection<ChzzkCategoryAlias> Aliases { get; set; } = new List<ChzzkCategoryAlias>();
    }
}
