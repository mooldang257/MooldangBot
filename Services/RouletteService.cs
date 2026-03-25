using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using MooldangAPI.ApiClients;
using Microsoft.Extensions.Caching.Memory;

namespace MooldangAPI.Services
{
    public class RouletteService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<OverlayHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ChzzkApiClient _chzzkApi;
        private readonly ILogger<RouletteService> _logger;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public RouletteService(AppDbContext db, IHubContext<OverlayHub> hubContext, IServiceProvider serviceProvider, ChzzkApiClient chzzkApi, ILogger<RouletteService> logger, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _db = db;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _chzzkApi = chzzkApi;
            _logger = logger;
            _cache = cache;
        }

        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            var results = await SpinRouletteMultiAsync(chzzkUid, rouletteId, 1, viewerNickname);
            return results.FirstOrDefault();
        }

        public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
            return await SpinRouletteMultiAsync(chzzkUid, rouletteId, 10, viewerNickname);
        }

        public async Task<List<RouletteItem>> SpinRouletteMultiAsync(string chzzkUid, int rouletteId, int count, string? viewerNickname = null)
        {
            if (count <= 0) return new List<RouletteItem>();

            await _semaphore.WaitAsync();
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var roulette = await _db.Roulettes
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

                if (roulette == null)
                    return new List<RouletteItem>();

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
                    return new List<RouletteItem>();
                }

                var results = new List<RouletteItem>();
                var logs = new List<RouletteLog>();
                bool is10x = count >= 10; 

                for (int i = 0; i < count; i++)
                {
                    var result = DrawItem(activeItems, is10x);
                    results.Add(result);

                    logs.Add(new RouletteLog
                    {
                        ChzzkUid = chzzkUid,
                        ViewerNickname = viewerNickname ?? "비회원",
                        ItemName = result.ItemName,
                        IsMission = result.IsMission,
                        Status = result.IsMission ? RouletteLogStatus.Pending : RouletteLogStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        ProcessedAt = result.IsMission ? null : DateTime.UtcNow
                    });
                }

                _db.RouletteLogs.AddRange(logs);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // 🏷️ 요약 데이터 생성 (v6: xCount 포맷 대응)
                var summary = results.GroupBy(r => r.ItemName)
                    .Select(g => {
                        var first = g.First();
                        return new RouletteSummaryDto(g.Key, g.Count(), first.IsMission, first.Color);
                    }).ToList();

                // 🏷️ SpinId 생성 및 결과 캐싱 (지연 알림용)
                string SpinId = Guid.NewGuid().ToString();
                var Context = new Controllers.SpinResultContext
                {
                    ChzzkUid = chzzkUid,
                    RouletteName = roulette.Name,
                    ViewerNickname = viewerNickname,
                    WinningItems = results.Select(r => r.ItemName).ToList()
                };
                _cache.Set($"Spin:{SpinId}", Context, TimeSpan.FromMinutes(1));

                // v6 DTO 응답
                var response = new SpinRouletteResponse(
                    SpinId,
                    rouletteId,
                    roulette.Name,
                    viewerNickname,
                    results.Select(r => new RouletteResultDto(r.ItemName, r.IsMission, r.Color, viewerNickname)).ToList(),
                    summary
                );

                await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveRouletteResult", response);

                // 실시간 미션 대시보드 업데이트
                foreach(var log in logs.Where(l => l.IsMission))
                {
                    await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("MissionReceived", log);
                }

                return results;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"🎰 [룰렛 {count}회 실행 중 오류 발생] 트랜잭션 롤백됨.");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private RouletteItem DrawItem(List<RouletteItem> items, bool is10x)
        {
            double totalWeight = items.Sum(i => is10x ? i.Probability10x : i.Probability);
            
            if (totalWeight <= 0)
            {
                _logger.LogError($"🎰 [룰렛 추첨 오류] {items.First().RouletteId}번 룰렛의 활성 항목 가중치 합이 0입니다.");
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

        public async Task SendDelayedChatResultAsync(Controllers.SpinResultContext Context)
        {
            try
            {
                string nickPrefix = string.IsNullOrEmpty(Context.ViewerNickname) ? "관리자테스트" : Context.ViewerNickname;
                
                // v6 규칙: 무조건 [항목명] x수량 포맷 (수량이 1이어도 표시)
                var grouped = Context.WinningItems.GroupBy(name => name)
                                     .Select(g => $"[{g.Key}] x{g.Count()}");
                
                string resultStr = string.Join(", ", grouped);
                string message = $"{nickPrefix}({Context.RouletteName})> {resultStr}";

                await SendChatMessageAsync(Context.ChzzkUid, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "룰렛 지연 결과 채팅 전송 중 오류 발생");
            }
        }
    }
}
