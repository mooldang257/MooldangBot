using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Infrastructure.ApiClients
{
    /// <summary>
    /// [GatewayChatClientProxy]: 기존 IChzzkChatClient 인터페이스를 유지하며 게이트웨이로 위임합니다.
    /// 네임스페이스 모호성 해결을 위해 FQCN을 사용합니다.
    /// </summary>
    public class GatewayChatClientProxy(HttpClient httpClient, ILogger<GatewayChatClientProxy> logger) : MooldangBot.Application.Interfaces.IChzzkChatClient
    {
        private readonly HttpClient _client = httpClient;
        private readonly ILogger<GatewayChatClientProxy> _logger = logger;

        public bool IsConnected(string chzzkUid) => true; 
        public bool HasAuthError(string chzzkUid) => false;

        public event Action<string, string, string>? OnChatMessageReceived;
        public event Action<string, string>? OnDonationReceived;

        public Task InitializeAsync()
        {
            _logger.LogInformation("[Gateway Proxy] InitializeAsync: Gateway handling sharding internally.");
            return Task.CompletedTask;
        }

        public Task<bool> ConnectAsync(string chzzkUid, string accessToken, string? clientId = null, string? clientSecret = null)
        {
            _logger.LogInformation("[Gateway Proxy] ConnectAsync via Gateway: {ChzzkUid}", chzzkUid);
            return Task.FromResult(true);
        }

        public Task DisconnectAsync(string chzzkUid)
        {
            _logger.LogInformation("[Gateway Proxy] DisconnectAsync: {ChzzkUid}", chzzkUid);
            return Task.CompletedTask;
        }

        public int GetActiveConnectionCount() => 0; 

        public IEnumerable<MooldangBot.Application.Interfaces.ShardStatus> GetShardStatuses()
        {
            _logger.LogWarning("[Gateway Proxy] GetShardStatuses is not yet implemented in Gateway. Returning empty list.");
            return Enumerable.Empty<MooldangBot.Application.Interfaces.ShardStatus>();
        }

        public async Task<bool> SendMessageAsync(string chzzkUid, string message)
        {
            try 
            {
                var response = await _client.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/message", new { Content = message });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Gateway Proxy] Failed to send message via gateway.");
                return false;
            }
        }

        public async Task<bool> SendNoticeAsync(string chzzkUid, string message)
        {
            var response = await _client.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/notice", new { Content = message });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTitleAsync(string chzzkUid, string newTitle)
        {
            var response = await _client.PostAsJsonAsync($"/api/internal/channels/{chzzkUid}/live-settings", new { Title = newTitle });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCategoryAsync(string chzzkUid, string category)
        {
            var response = await _client.PostAsJsonAsync($"/api/internal/channels/{chzzkUid}/live-settings", new { CategoryId = category });
            return response.IsSuccessStatusCode;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void EmitTestMessage(string sender, string message) => OnChatMessageReceived?.Invoke(sender, message, "user");
    }
}
