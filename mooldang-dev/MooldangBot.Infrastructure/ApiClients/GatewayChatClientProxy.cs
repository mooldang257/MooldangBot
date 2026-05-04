using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Contracts.Chzzk.Models;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.Infrastructure.ApiClients
{
    /// <summary>
    /// [GatewayChatClientProxy]: 기존 IChzzkChatClient 인터페이스를 유지하며 게이트웨이로 위임합니다.
    /// 네임스페이스 모호성 해결을 위해 FQCN을 사용합니다.
    /// </summary>
    public class GatewayChatClientProxy(IHttpClientFactory httpClientFactory, ILogger<GatewayChatClientProxy> logger) : MooldangBot.Domain.Abstractions.IChzzkChatClient
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<GatewayChatClientProxy> _logger = logger;
        private HttpClient GetClient() => _httpClientFactory.CreateClient("ChzzkGateway");

        public bool IsConnected(string ChzzkUid) => true; 
        public bool HasAuthError(string ChzzkUid) => false;

        public event Action<string, string, string>? OnChatMessageReceived;
#pragma warning disable 0067
        public event Action<string, string>? OnDonationReceived;
#pragma warning restore 0067

        public Task InitializeAsync()
        {
            _logger.LogInformation("[Gateway Proxy] InitializeAsync: Gateway handling sharding internally.");
            return Task.CompletedTask;
        }

        public Task<bool> ConnectAsync(string ChzzkUid, string AccessToken, string? ClientId = null, string? ClientSecret = null)
        {
            _logger.LogInformation("[Gateway Proxy] ConnectAsync via Gateway: {ChzzkUid}", ChzzkUid);
            return Task.FromResult(true);
        }
 
        public Task DisconnectAsync(string ChzzkUid)
        {
            _logger.LogInformation("[Gateway Proxy] DisconnectAsync: {ChzzkUid}", ChzzkUid);
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
                var Client = GetClient();
                var Response = await Client.GetFromJsonAsync<ChzzkApiResponse<IEnumerable<ShardStatus>>>("/api/internal/shards/status");
 
                if (Response != null && Response.IsSuccess && Response.Content != null)
                {
                    var StatusList = Response.Content.ToList();
                    _logger.LogInformation("📡 [Gateway Proxy] {Count}개의 샤드 상태를 수신했습니다.", StatusList.Count);
                    return StatusList;
                }
 
                _logger.LogWarning("⚠️ [Gateway Proxy] 샤드 상태 조회 실패 또는 데이터가 비어있습니다. Code: {Code}, Message: {Message}", 
                    Response?.Code, Response?.Message);
                return Enumerable.Empty<ShardStatus>();
            }
            catch (Exception Ex)
            {
                _logger.LogError(Ex, "❌ [Gateway Proxy] 샤드 상태 조회 중 통신 오류 발생.");
                return Enumerable.Empty<ShardStatus>();
            }
        }

        public async Task<bool> SendMessageAsync(string ChzzkUid, string Message)
        {
            try 
            {
                var Client = GetClient();
                var Response = await Client.PostAsJsonAsync($"/api/internal/chat/{ChzzkUid}/message", new { Content = Message });
                return Response.IsSuccessStatusCode;
            }
            catch (Exception Ex)
            {
                _logger.LogError(Ex, "[Gateway Proxy] Failed to send message via gateway.");
                return false;
            }
        }
 
        public async Task<bool> SendNoticeAsync(string ChzzkUid, string Message)
        {
            var Client = GetClient();
            var Response = await Client.PostAsJsonAsync($"/api/internal/chat/{ChzzkUid}/notice", new { Content = Message });
            return Response.IsSuccessStatusCode;
        }
 
        public async Task<bool> UpdateTitleAsync(string ChzzkUid, string NewTitle)
        {
            var Client = GetClient();
            var Response = await Client.PostAsJsonAsync($"/api/internal/channels/{ChzzkUid}/live-settings", new { Title = NewTitle });
            return Response.IsSuccessStatusCode;
        }
 
        public async Task<bool> UpdateCategoryAsync(string ChzzkUid, string Category)
        {
            var Client = GetClient();
            var Response = await Client.PostAsJsonAsync($"/api/internal/channels/{ChzzkUid}/live-settings", new { CategoryId = Category });
            return Response.IsSuccessStatusCode;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void EmitTestMessage(string Sender, string Message) => OnChatMessageReceived?.Invoke(Sender, Message, "user");
    }
}
