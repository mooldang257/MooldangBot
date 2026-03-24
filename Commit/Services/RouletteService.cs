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

        public RouletteService(AppDbContext db, IHubContext<OverlayHub> hubContext, IServiceProvider serviceProvider, ChzzkApiClient chzzkApi)
        {
            _db = db;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _chzzkApi = chzzkApi;
        }

        /// <summary>
        /// 룰렛 1회 추첨을 실행합니다.
        /// </summary>
        public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null || !roulette.Items.Any())
                return null;

            var result = DrawItem(roulette.Items, is10x: false);
            
            // SignalR 알림 전송 (소문자 그룹명 사용)
            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = new List<RouletteItem> { result }
            });

            // 봇 채팅 전송
            var resultList = new List<RouletteItem> { result };
            _ = SendChatResultAsync(chzzkUid, roulette.Name, viewerNickname, resultList);

            return result;
        }

        /// <summary>
        /// 룰렛 10연차 추첨을 실행합니다.
        /// </summary>
        public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null || !roulette.Items.Any())
                return new List<RouletteItem>();

            var results = new List<RouletteItem>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(DrawItem(roulette.Items, is10x: true));
            }

            // SignalR 알림 전송 (소문자 그룹명 사용)
            await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = results
            });

            // 봇 채팅 전송
            _ = SendChatResultAsync(chzzkUid, roulette.Name, viewerNickname, results);

            return results;
        }

        /// <summary>
        /// 가중치 기반 랜덤 추첨 로직
        /// </summary>
        private RouletteItem DrawItem(List<RouletteItem> items, bool is10x)
        {
            double totalWeight = items.Sum(i => is10x ? i.Probability10x : i.Probability);
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

        private async Task SendChatResultAsync(string chzzkUid, string rouletteName, string? viewerNickname, List<RouletteItem> results)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var streamer = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkAccessToken)) 
                    return;

                string nickPrefix = string.IsNullOrEmpty(viewerNickname) ? "관리자테스트" : viewerNickname;
                
                // 결과 문자열 조립: 항목1x2, 항목2, 항목6x4
                var grouped = results.GroupBy(r => r.ItemName)
                                     .Select(g => g.Count() > 1 ? $"{g.Key}x{g.Count()}" : g.Key);
                string resultStr = string.Join(", ", grouped);

                string message = $"{nickPrefix}({rouletteName})> {resultStr}";

                await _chzzkApi.SendChatMessageAsync(streamer.ChzzkAccessToken, message);
            }
            catch
            {
                // 실패 시 무시 (오버레이는 정상 작동하게 둠)
            }
        }
    }
}
