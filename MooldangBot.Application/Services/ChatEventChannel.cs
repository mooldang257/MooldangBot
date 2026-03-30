using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;

namespace MooldangBot.Application.Services;

/// <summary>
/// [Phase1: 역압 처리] Bounded Channel 기반의 채팅 이벤트 큐 서비스입니다.
/// 최대 2,000건의 이벤트를 버퍼링하며, 큐가 가득 차면 가장 오래된 이벤트를 드롭합니다.
/// </summary>
public sealed class ChatEventChannel : IChatEventChannel
{
    private readonly Channel<ChatEventItem> _channel;
    private readonly ILogger<ChatEventChannel> _logger;

    public ChatEventChannel(ILogger<ChatEventChannel> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<ChatEventItem>(new BoundedChannelOptions(2000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = false  // 다중 Consumer 지원
        });
    }

    public bool TryWrite(ChatEventItem item)
    {
        bool written = _channel.Writer.TryWrite(item);
        if (!written)
        {
            _logger.LogWarning($"⚠️ [역압 경고] 채팅 이벤트 큐가 포화 상태입니다. 가장 오래된 이벤트가 드롭됩니다. (채널: {item.ChzzkUid})");
        }
        return written;
    }

    public IAsyncEnumerable<ChatEventItem> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    public int PendingCount => _channel.Reader.Count;
}
