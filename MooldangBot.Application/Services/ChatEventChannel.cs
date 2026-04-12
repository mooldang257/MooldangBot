using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Chat;

namespace MooldangBot.Application.Services;

/// <summary>
/// [오시리스의 Bridge]: 10k RPS 폭주를 흡수하는 고속 메모리 채널 서비스입니다. (Phase 2 최종형)
/// </summary>
public sealed class ChatEventChannel : IChatEventChannel
{
    private readonly Channel<ChatEventPacket> _channel;
    private readonly ILogger<ChatEventChannel> _logger;

    public ChatEventChannel(ILogger<ChatEventChannel> logger)
    {
        _logger = logger;
        
        // [물멍 가이드]: 10만 건의 버퍼로 10초간의 폭주(10k RPS)를 수용
        var options = new BoundedChannelOptions(100000)
        {
            FullMode = BoundedChannelFullMode.Wait, // 유실 방지를 위한 대기 모드
            SingleWriter = false,
            SingleReader = true   // 처리 워커(ChzzkEventProcessingWorker)가 하나이므로 true로 성능 극대화
        };

        _channel = Channel.CreateBounded<ChatEventPacket>(options);
    }

    public bool TryWrite(ChatEventPacket packet)
    {
        bool written = _channel.Writer.TryWrite(packet);
        if (!written)
        {
            // 사실상 FullMode.Wait이므로 TryWrite는 큐가 찰 때만 false를 반환하지만, 
            // 안전 차원에서 로그를 남깁니다.
            _logger.LogWarning($"⚠️ [Bridge 지연] 채널 {packet.StreamerChzzkUid}의 이벤트를 버퍼에 담지 못했습니다.");
        }
        return written;
    }

    public IAsyncEnumerable<ChatEventPacket> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    public int PendingCount => _channel.Reader.Count;
}
