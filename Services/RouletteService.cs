using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using MooldangAPI.ApiClients;

namespace MooldangAPI.Services
{
    public class RouletteService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ChzzkApiClient _chzzkApi;
        private readonly ILogger<RouletteService> _logger;

        public RouletteService(AppDbContext db, IHubContext<OverlayHub> hubContext, IServiceProvider serviceProvider, ChzzkApiClient chzzkApi, ILogger<RouletteService> logger)
        {
            _db = db;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _chzzkApi = chzzkApi;
            _logger = logger;
        }

        public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null)
                return null;

            // 🚨 활성화된 항목이 있는지 검사 (방어 코드)
            var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
            if (!activeItems.Any())
            {
                _logger.LogWarning($"🎰 [룰렛 실행 실패] {rouletteId}번에 활성화된 항목이 없습니다.");
                try
                {
                    await SendChatMessageAsync(chzzkUid, "⚠️ 현재 활성화된 항목이 없어 룰렛을 돌릴 수 없습니다. 관리 페이지에서 항목을 활성화해 주세요!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "비활성화 안내 메시지 전송 중 오류 발생");
                }
                return null;
            }

            var result = DrawItem(activeItems, is10x: false);
            
            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = new List<RouletteItem> { result }
            });

            var resultList = new List<RouletteItem> { result };
            _ = SendChatResultAsync(chzzkUid, roulette.Name, viewerNickname, resultList);

            return result;
        }

        public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null)
                return new List<RouletteItem>();

            var activeItems = roulette.Items.Where(i => i.IsActive).ToList();
            if (!activeItems.Any())
            {
                _logger.LogWarning($"🎰 [룰렛 10연차 실행 실패] {rouletteId}번에 활성화된 항목이 없습니다.");
                try
                {
                    await SendChatMessageAsync(chzzkUid, "⚠️ 현재 활성화된 항목이 없어 룰렛을 돌릴 수 없습니다. 관리 페이지에서 항목을 활성화해 주세요!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "비활성화 안내 메시지 전송 중 오류 발생");
                }
                return new List<RouletteItem>();
            }

            var results = new List<RouletteItem>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(DrawItem(activeItems, is10x: true));
            }

            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = results
            });

            _ = SendChatResultAsync(chzzkUid, roulette.Name, viewerNickname, results);

            return results;
        }

        private RouletteItem DrawItem(List<RouletteItem> items, bool is10x)
        {
            double totalWeight = items.Sum(i => is10x ? i.Probability10x : i.Probability);
            
            // 🔍 로그 강화: 확률 합계가 0인 경우 기록
            if (totalWeight <= 0)
            {
                _logger.LogError($"🎰 [룰렛 추첨 오류] {items.First().RouletteId}번 룰렛의 활성 항목 가중치 합이 0입니다. 첫 번째 항목으로 강제 당첨 처리합니다.");
                return items.First();
            }

            double randomValue = Random.Shared.NextDouble() * totalWeight;
            double cursor = 0;

            foreach (var item in items)
            {
                cursor += is10x ? item.Probability10x : item.Probability;
                if (randomValue <= cursor)
                {
                    return item;
                }
            }

            return items.Last();
        }

        private async Task SendChatMessageAsync(string chzzkUid, string message)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkAccessToken))
                return;

            await _chzzkApi.SendChatMessageAsync(streamer.ChzzkAccessToken, message);
        }

        private async Task SendChatResultAsync(string chzzkUid, string rouletteName, string? viewerNickname, List<RouletteItem> results)
        {
            try
            {
                string nickPrefix = string.IsNullOrEmpty(viewerNickname) ? "관리자테스트" : viewerNickname;
                var grouped = results.GroupBy(r => r.ItemName)
                                     .Select(g => g.Count() > 1 ? $"{g.Key}x{g.Count()}" : g.Key);
                string resultStr = string.Join(", ", grouped);
                string message = $"{nickPrefix}({rouletteName})> {resultStr}";

                await SendChatMessageAsync(chzzkUid, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "룰렛 결과 채팅 전송 중 오류 발생");
            }
        }
    }
}
