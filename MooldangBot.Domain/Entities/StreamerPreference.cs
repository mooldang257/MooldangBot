using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [오시리스의 인장]: 스트리머별 영구적인 개인화 설정(Preference)을 저장하는 엔티티입니다.
/// </summary>
public class StreamerPreference : IAuditable
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 대상 스트리머의 고유 식별자 (FK)
    /// null인 경우 시스템 전역 설정을 의미합니다.
    /// </summary>
    public int? StreamerProfileId { get; set; }

    /// <summary>
    /// 설정 키 (예: "DarkMode", "TableRowsPerPage")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PreferenceKey { get; set; } = null!;

    /// <summary>
    /// 설정 값. 선장님의 지시대로 유연함을 위해 TEXT 형식으로 저장합니다.
    /// </summary>
    [Required]
    public string PreferenceValue { get; set; } = null!;

    // audit fields
    public KstClock CreatedAt { get; set; }
    public KstClock? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(StreamerProfileId))]
    public StreamerProfile? StreamerProfile { get; set; }
}
