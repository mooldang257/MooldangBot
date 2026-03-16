using System.ComponentModel.DataAnnotations;

namespace MooldangAPI.Models
{
    // 시스템 전체 공통 설정을 보관하는 마스터 테이블
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string KeyName { get; set; } = string.Empty; // 예: "ChzzkClientId"

        public string KeyValue { get; set; } = string.Empty; // 실제 키 값
    }
}