using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;

namespace MooldangAPI.Services
{
    public class RouletteService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;

        public RouletteService(AppDbContext db, IHubContext<OverlayHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        /// <summary>
        /// 룰렛 1회 추첨을 실행합니다.
        /// </summary>
        public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId)
        {
            var roulette = await _db.Roulettes
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

            if (roulette == null || !roulette.Items.Any())
                return null;

            var result = DrawItem(roulette.Items, is10x: false);
            
            // SignalR 알림 전송
            await _hubContext.Clients.Group(chzzkUid).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = new List<RouletteItem> { result }
            });

            return result;
        }

        /// <summary>
        /// 룰렛 10연차 추첨을 실행합니다.
        /// </summary>
        public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId)
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

            // SignalR 알림 전송
            await _hubContext.Clients.Group(chzzkUid).SendAsync("RouletteTriggered", new
            {
                RouletteId = rouletteId,
                RouletteName = roulette.Name,
                Results = results
            });

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
    }
}
