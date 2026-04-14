using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Chzzk.Models;
using MooldangBot.Contracts.Chzzk.Models.Events;
using MooldangBot.Contracts.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.Infrastructure.ApiClients
{
    /// <summary>
    /// [GatewayChatClientProxy]: 기존 IChzzkChatClient 인터페이스를 유지하며 게이트웨이로 위임합니다.
    /// 네임스페이스 모호성 해결을 위해 FQCN을 사용합니다.
    /// </summary>
    public class GatewayChatClientProxy(IHttpClientFactory httpClientFactory, ILogger<GatewayChatClientProxy> logger) : MooldangBot.Contracts.Common.Interfaces.IChzzkChatClient
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<GatewayChatClientProxy> _logger = logger;
        private HttpClient GetClient() => _httpClientFactory.CreateClient("ChzzkGateway");

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

        /// <summary>
        /// [오시리스의 지표]: 게이트웨이로부터 실시간 샤드 상태 정보를 가져옵니다.
        /// </summary>
        public async Task<IEnumerable<ShardStatus>> GetShardStatusesAsync()
        {
            try
            {
                var client = GetClient();
                var response = await client.GetFromJsonAsync<ChzzkApiResponse<IEnumerable<ShardStatus>>>("/api/internal/shards/status");

                if (response != null && response.IsSuccess && response.Content != null)
                {
                    var statusList = response.Content.ToList();
                    _logger.LogInformation("📡 [Gateway Proxy] {Count}개의 샤드 상태를 수신했습니다.", statusList.Count);
                    return statusList;
                }

                _logger.LogWarning("⚠️ [Gateway Proxy] 샤드 상태 조회 실패 또는 데이터가 비어있습니다. Code: {Code}, Message: {Message}", 
                    response?.Code, response?.Message);
                return Enumerable.Empty<ShardStatus>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Gateway Proxy] 샤드 상태 조회 중 통신 오류 발생.");
                return Enumerable.Empty<ShardStatus>();
            }
        }

        public async Task<bool> SendMessageAsync(string chzzkUid, string message)
        {
            try 
            {
                var client = GetClient();
                var response = await client.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/message", new { Content = message });
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
            var client = GetClient();
            var response = await client.PostAsJsonAsync($"/api/internal/chat/{chzzkUid}/notice", new { Content = message });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTitleAsync(string chzzkUid, string newTitle)
        {
            var client = GetClient();
            var response = await client.PostAsJsonAsync($"/api/internal/channels/{chzzkUid}/live-settings", new { Title = newTitle });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCategoryAsync(string chzzkUid, string category)
        {
            var client = GetClient();
            var response = await client.PostAsJsonAsync($"/api/internal/channels/{chzzkUid}/live-settings", new { CategoryId = category });
            return response.IsSuccessStatusCode;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void EmitTestMessage(string sender, string message) => OnChatMessageReceived?.Invoke(sender, message, "user");
    }
}
