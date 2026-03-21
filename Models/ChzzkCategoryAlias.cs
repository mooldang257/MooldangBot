using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MooldangAPI.Models
{
    public class ChzzkCategoryAlias
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Alias { get; set; } = string.Empty;

        [ForeignKey("CategoryId")]
        public ChzzkCategory? Category { get; set; }
    }
}
