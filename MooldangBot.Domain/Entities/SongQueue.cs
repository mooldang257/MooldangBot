using System.ComponentModel.DataAnnotations;

namespace MooldangBot.Domain.Entities
{
    public class SongQueue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChzzkUid { get; set; } = string.Empty; // 이 곡이 어떤 스트리머의 대기열인지 식별

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // 곡 제목

        [MaxLength(100)]
        public string? Artist { get; set; } // 가수

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // 상태: Pending(대기), Playing(재생중), Completed(완료)

        public int SortOrder { get; set; } = 0; // 드래그 앤 드롭 정렬을 기억하기 위한 순서 번호

        public DateTime CreatedAt { get; set; } = DateTime.Now; // 신청된 시간
    }
}