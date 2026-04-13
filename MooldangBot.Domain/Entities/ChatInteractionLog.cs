using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [서기의 기록]: 모든 채팅 및 후원 메시지 상호작용을 기록하는 엔티티입니다. (v3.7 신설)
/// </summary>
[Table("log_chat_interactions")]
[Index(nameof(StreamerProfileId), nameof(CreatedAt))]
[Index(nameof(IsCommand), nameof(CreatedAt))]
public class ChatInteractionLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int StreamerProfileId { get; set; }

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual StreamerProfile? StreamerProfile { get; set; }

    [Required]
    [MaxLength(100)]
    public string SenderNickname { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsCommand { get; set; }

    /// <summary>
    /// [존재의 분류]: 'Chat' 또는 'Donation'
    /// </summary>
    [MaxLength(20)]
    public string MessageType { get; set; } = "Chat";

    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
