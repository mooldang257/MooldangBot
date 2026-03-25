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
            await _semaphore.WaitAsync();
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var roulette = await _db.Roulettes
                    .Include(r => r.Items)
                    .FirstOrDefaultAsync(r => r.Id == rouletteId && r.ChzzkUid == chzzkUid && r.IsActive);

                if (roulette == null)
                    return null;

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

                // 📝 로그 기록
                var log = new RouletteLog
                {
                    ChzzkUid = chzzkUid,
                    ViewerNickname = viewerNickname ?? "비회원",
                    ItemName = result.ItemName,
                    IsMission = result.IsMission,
                    Status = result.IsMission ? RouletteLogStatus.Pending : RouletteLogStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = result.IsMission ? null : DateTime.UtcNow
                };
                _db.RouletteLogs.Add(log);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // 🏷️ SpinId 생성 및 결과 캐싱 (지연 알림용)
                string SpinId = Guid.NewGuid().ToString();
                var Context = new Controllers.SpinResultContext
                {
                    ChzzkUid = chzzkUid,
                    RouletteName = roulette.Name,
                    ViewerNickname = viewerNickname,
                    WinningItems = new List<string> { result.ItemName }
                };
                _cache.Set($"Spin:{SpinId}", Context, TimeSpan.FromMinutes(1));

                await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveRouletteResult", new
                {
                    SpinId = SpinId,
                    RouletteId = rouletteId,
                    RouletteName = roulette.Name,
                    Results = new List<RouletteItem> { result }
                });

                // 실시간 미션 대시보드 업데이트
                await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("MissionReceived", log);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "🎰 [룰렛 실행 중 오류 발생] 트랜잭션 롤백됨.");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null)
        {
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
                var logs = new List<RouletteLog>();

                for (int i = 0; i < 10; i++)
                {
                    var result = DrawItem(activeItems, is10x: true);
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

                await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveRouletteResult", new
                {
                    SpinId = SpinId,
                    RouletteId = rouletteId,
                    RouletteName = roulette.Name,
                    Results = results
                });

                // 실시간 미션 대시보드 업데이트 (병렬 전송)
                foreach(var log in logs)
                {
                    await _hubContext.Clients.Group(chzzkUid.ToLower()).SendAsync("MissionReceived", log);
                }

                return results;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "🎰 [룰렛 10연차 실행 중 오류 발생] 트랜잭션 롤백됨.");
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
            // 하위 호환성 유지
            var context = new Controllers.SpinResultContext
            {
                ChzzkUid = chzzkUid,
                RouletteName = rouletteName,
                ViewerNickname = viewerNickname,
                WinningItems = results.Select(r => r.ItemName).ToList()
            };
            await SendDelayedChatResultAsync(context);
        }

        public async Task SendDelayedChatResultAsync(Controllers.SpinResultContext Context)
        {
            try
            {
                string nickPrefix = string.IsNullOrEmpty(Context.ViewerNickname) ? "관리자테스트" : Context.ViewerNickname;
                var grouped = Context.WinningItems.GroupBy(name => name)
                                     .Select(g => g.Count() > 1 ? $"[{g.Key}] x{g.Count()}" : $"[{g.Key}]");
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
