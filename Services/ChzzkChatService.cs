using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using SocketIOClient;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MooldangAPI.Services
{
    public class ChzzkChatService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<OverlayHub> _hubContext;
        private readonly ILogger<ChzzkChatService> _logger;
        private readonly HttpClient _httpClient;
        // 💡 클라이언트 타입을 SocketIO로 변경하고, ConcurrentDictionary에 맞게 세팅합니다.
        private readonly ConcurrentDictionary<string, SocketIOClient.SocketIO> _activeConnections = new();

        public ChzzkChatService(IServiceScopeFactory scopeFactory, IHubContext<OverlayHub> hubContext, ILogger<ChzzkChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 [최종 코어] 엔진 가동... DB 체크 시작");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. DB에 등록된 스트리머가 몇 명인지부터 확인
                var profiles = await db.StreamerProfiles.ToListAsync();
                // _logger.LogInformation($"[엔진] 현재 DB에 등록된 스트리머 수: {profiles.Count}명");

                foreach (var profile in profiles)
                {
                    if (string.IsNullOrEmpty(profile.ChzzkUid)) continue;

                    if (!_activeConnections.ContainsKey(profile.ChzzkUid))
                    {
                        // ⭐ 상세 로그 추가: 토큰 상태를 터미널에 바로 찍습니다.
                        if (string.IsNullOrEmpty(profile.ChzzkAccessToken))
                        {
                            _logger.LogWarning($"[엔진] {profile.ChzzkUid} -> ❌ 토큰 없음 (DB 확인 필요)");
                        }
                        else
                        {
                            _logger.LogInformation($"[엔진] {profile.ChzzkUid} -> 💎 토큰 발견! 접속 시도...");

                            var clientIdConfig = await db.SystemSettings.FindAsync("ChzzkClientId");
                            var clientSecretConfig = await db.SystemSettings.FindAsync("ChzzkClientSecret");

                            if (clientIdConfig == null)
                            {
                                _logger.LogError("[엔진] ❌ 마스터 ClientId가 systemsettings 테이블에 없습니다!");
                            }
                            else
                            {
                               // _ = ConnectToChzzkChat(profile, clientIdConfig.KeyValue, clientSecretConfig?.KeyValue ?? "", stoppingToken);
                            }
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    _logger.LogInformation("🚀 [최종 코어] 치지직 공식 인증(OAuth) 채팅 엔진 가동 시작...");

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        using var scope = _scopeFactory.CreateScope();
        //        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        //        var clientIdConfig = await db.SystemSettings.FindAsync("ChzzkClientId");
        //        var clientSecretConfig = await db.SystemSettings.FindAsync("ChzzkClientSecret");
        //        string masterClientId = clientIdConfig?.KeyValue ?? "";
        //        string masterClientSecret = clientSecretConfig?.KeyValue ?? "";

        //        if (!string.IsNullOrEmpty(masterClientId) && !string.IsNullOrEmpty(masterClientSecret))
        //        {
        //            var profiles = await db.StreamerProfiles.Where(p => !string.IsNullOrEmpty(p.ChzzkUid)).ToListAsync();
        //            foreach (var profile in profiles)
        //            {
        //                if (!_activeConnections.ContainsKey(profile.ChzzkUid!))
        //                {
        //                    // ⭐ 토큰이 없는 스트리머는 연결을 시도하지 않고 경고만 띄웁니다.
        //                    if (string.IsNullOrEmpty(profile.ChzzkAccessToken))
        //                    {
        //                        _logger.LogWarning($"⚠️ [{profile.ChzzkUid}] 인증 토큰이 없습니다. 브라우저에서 치지직 로그인을 진행해주세요.");
        //                        continue;
        //                    }

        //                    _logger.LogInformation($"🔌 [{profile.ChzzkUid}] 공식 인증 서버를 통해 채팅 서버 접속 시도 중...");
        //                    _ = ConnectToChzzkChat(profile, masterClientId, masterClientSecret, stoppingToken);
        //                }
        //            }
        //        }

        //        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        //    }
        //}

        //private async Task ConnectToChzzkChat(StreamerProfile profile, string clientId, string clientSecret, CancellationToken stoppingToken)
        //{
        //    if (_activeConnections.ContainsKey(profile.ChzzkUid)) return;
        //    _activeConnections.TryAdd(profile.ChzzkUid, null!);

        //    try
        //    {
        //        _logger.LogInformation($"🚀 [{profile.ChzzkUid}] 공식 Open API 세션 연결 시도...");

        //        using var authClient = new HttpClient();
        //        authClient.DefaultRequestHeaders.Add("Client-Id", clientId);
        //        authClient.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        //        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

        //        var authRes = await authClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/sessions/auth");
        //        var authJson = await authRes.Content.ReadAsStringAsync();

        //        if (!authRes.IsSuccessStatusCode)
        //        {
        //            _logger.LogError($"❌ 세션 발급 실패: {authJson}");
        //            _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //            return;
        //        }

        //        using var authDoc = JsonDocument.Parse(authJson);
        //        string socketUrl = authDoc.RootElement.GetProperty("content").GetProperty("url").GetString() ?? "";
        //        Uri uri = new Uri(socketUrl);
        //        string authKey = System.Web.HttpUtility.ParseQueryString(uri.Query)["auth"] ?? "";

        //        // ⭐ [치트키 1] wss:// 가 아닌 https:// 로 기본 주소를 잡습니다. (포트 443 제거)
        //        string baseUri = $"https://{uri.Host}";

        //        var query = new NameValueCollection();
        //        query.Add("auth", authKey);

        //        var options = new SocketIOOptions
        //        {
        //            EIO = EngineIO.V3,
        //            Path = "/",  // 👈 치지직은 루트 경로를 사용합니다.
        //            Query = query,
        //            ConnectionTimeout = TimeSpan.FromSeconds(7), // 너무 오래 기다리지 않게 단축
        //                                                         // ⭐ [치트키 2] 폴링을 시도조차 못 하게 하고 바로 웹소켓으로 꽂습니다.
        //            Transport = SocketIOClient.TransportProtocol.WebSocket
        //        };

        //        options.ExtraHeaders = new Dictionary<string, string> {
        //    { "User-Agent", "Mozilla/5.0" },
        //    { "Origin", "https://chzzk.naver.com" }
        //};

        //        var client = new SocketIOClient.SocketIO(new Uri(baseUri), options);

        //        // --- 이벤트 등록 (기존과 동일하지만 간단하게 요약) ---
        //        client.OnConnected += (s, e) => _logger.LogInformation("✅ 물리 연결 성공!");
        //        client.On("SYSTEM", async r => { /* ... 세션키 받고 구독하는 로직 ... */ });
        //        client.On("CHAT", r => { /* ... 채팅 로직 ... */ return Task.CompletedTask; });

        //        // ⭐ [치트키 3] Deadlock 방지: ConnectAsync를 별도 태스크로 분리하여 실행합니다.
        //        _logger.LogWarning($"👉 [추적 3] {baseUri} 에 {authKey.Substring(0, 5)}... 키로 돌격합니다.");

        //        // Task.Run으로 감싸서 현재 스레드가 얼어붙는 것을 방지합니다.
        //        await Task.Run(async () => await client.ConnectAsync().ConfigureAwait(false));

        //        _logger.LogWarning("👉 [추적 4] 드디어 통과! 이제 채팅 대기 모드입니다.");
        //        _activeConnections[profile.ChzzkUid] = client;

        //        while (client.Connected && !stoppingToken.IsCancellationRequested)
        //        {
        //            await Task.Delay(2000, stoppingToken);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"❌ 최종 실패: {ex.Message}");
        //    }
        //    finally
        //    {
        //        _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //    }
        //}

        //// ==========================================
        //// 🌐 치지직 공식 Open API 웹소켓 연결 코어
        //// ==========================================
        //private async Task ConnectToChzzkChat(StreamerProfile profile, string clientId, string clientSecret, CancellationToken stoppingToken)
        //{
        //    if (_activeConnections.ContainsKey(profile.ChzzkUid)) return;
        //    _activeConnections.TryAdd(profile.ChzzkUid, null!);

        //    try
        //    {
        //        _logger.LogInformation($"🚀 [{profile.ChzzkUid}] 공식 Open API 세션 연결을 시작합니다...");

        //        // [조명탄 1]
        //        _logger.LogWarning("👉 [추적 1] 네이버에 HTTP 세션 주소를 요청합니다...");
        //        using var authClient = new HttpClient();
        //        authClient.Timeout = TimeSpan.FromSeconds(15);
        //        authClient.DefaultRequestHeaders.Add("Client-Id", clientId);
        //        authClient.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        //        authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);
        //        authClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        //        var authRes = await authClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/sessions/auth");
        //        var authJson = await authRes.Content.ReadAsStringAsync();

        //        // [조명탄 2]
        //        _logger.LogWarning($"👉 [추적 2] HTTP 응답 완료 (상태 코드: {authRes.StatusCode})");

        //        if (!authRes.IsSuccessStatusCode)
        //        {
        //            _logger.LogError($"❌ [세션 발급 실패] {authJson}");
        //            _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //            return;
        //        }

        //        using var authDoc = JsonDocument.Parse(authJson);

        //        string socketUrl = authDoc.RootElement.GetProperty("content").GetProperty("url").GetString() ?? "";
        //        _logger.LogWarning($"👉 [추적 2.5] 원본 Socket URL: {socketUrl}");

        //        Uri uri = new Uri(socketUrl);
        //        string authKey = System.Web.HttpUtility.ParseQueryString(uri.Query)["auth"] ?? "";

        //        string baseUri = $"{uri.Scheme}://{uri.Host}";

        //        // ⭐ [타입 에러 해결] 라이브러리가 원하는 'NameValueCollection'을 직접 생성합니다.
        //        var queryCollection = new NameValueCollection();
        //        queryCollection.Add("auth", authKey); // 열쇠 주입

        //        var options = new SocketIOOptions
        //        {
        //            EIO = EngineIO.V3,
        //            Path = "/", // 네이버 소켓은 루트 경로를 사용합니다.

        //            // ⭐ [핵심] 이제 에러 없이 규격에 딱 맞는 데이터를 던져줍니다.
        //            Query = queryCollection,

        //            ConnectionTimeout = TimeSpan.FromSeconds(10)
        //        };

        //        options.ExtraHeaders = new Dictionary<string, string>
        //{
        //    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
        //    { "Origin", "https://chzzk.naver.com" }
        //};

        //        var client = new SocketIOClient.SocketIO(new Uri(baseUri), options);


        //        client.OnConnected += (sender, e) =>
        //        {
        //            _logger.LogInformation($"✅ [{profile.ChzzkUid}] 물리적 연결 성공! SYSTEM 이벤트를 기다립니다.");
        //        };

        //        // ⭐ [SYSTEM 이벤트] 세션 키 획득 및 채널 구독
        //        client.On("SYSTEM", async response =>
        //        {
        //            try
        //            {
        //                var data = response.GetValue<JsonElement>(0);
        //                string type = data.GetProperty("type").GetString() ?? "";

        //                if (type == "connected")
        //                {
        //                    string sessionKey = data.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
        //                    _logger.LogInformation($"💎 [Session Key 획득] {sessionKey}");

        //                    using var subClient = new HttpClient();
        //                    subClient.DefaultRequestHeaders.Add("Client-Id", clientId);
        //                    subClient.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        //                    subClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

        //                    var subReq = new { channelId = profile.ChzzkUid };
        //                    var content = new StringContent(JsonSerializer.Serialize(subReq), System.Text.Encoding.UTF8, "application/json");

        //                    var subRes = await subClient.PostAsync($"https://openapi.chzzk.naver.com/open/v1/sessions/events/subscribe/chat?sessionKey={sessionKey}", content);
        //                    string subResBody = await subRes.Content.ReadAsStringAsync();

        //                    if (subRes.IsSuccessStatusCode && subResBody.Contains("\"code\": 200"))
        //                    {
        //                        _logger.LogInformation($"🎉 [구독 완료] 이제 {profile.ChzzkUid} 채팅이 쏟아집니다!");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError($"❌ [구독 실패 응답] {subResBody}");
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError($"❌ [SYSTEM 에러] {ex.Message}");
        //            }
        //        });

        //        // ⭐ [CHAT 이벤트] 실제 명령어 인식
        //        client.On("CHAT", response =>
        //        {
        //            try
        //            {
        //                var chatData = response.GetValue<JsonElement>(0);
        //                string message = chatData.GetProperty("content").GetString() ?? "";

        //                string profileJson = chatData.GetProperty("profile").GetString() ?? "{}";
        //                using var profileDoc = JsonDocument.Parse(profileJson);
        //                string nickname = profileDoc.RootElement.GetProperty("nickname").GetString() ?? "시청자";

        //                _logger.LogInformation($"💬 [{nickname}]: {message}");

        //                string songCmd = profile.SongCommand ?? "!신청";
        //                if (message.StartsWith(songCmd) && message.Length > songCmd.Length)
        //                {
        //                    string songTitle = message.Substring(songCmd.Length).Trim();
        //                    _logger.LogInformation($"🎵 [곡 신청 포착] {nickname}님이 '{songTitle}'을(를) 신청했습니다!");
        //                }

        //                string omaCmd = profile.OmakaseCommand ?? "!물마카세";
        //                if (message.StartsWith(omaCmd))
        //                {
        //                    _logger.LogInformation($"🍣 [물마카세 포착] {nickname}님이 물마카세 버튼을 눌렀습니다!");
        //                }
        //            }
        //            catch { }
        //            return Task.CompletedTask;
        //        });

        //        // ⭐ [CCTV 가동] 알 수 없는 모든 이벤트를 잡아냅니다.
        //        client.OnAny((eventName, response) =>
        //        {
        //            string rawData = "데이터 없음";
        //            try
        //            {
        //                rawData = response.GetValue<JsonElement>(0).GetRawText();
        //            }
        //            catch
        //            {
        //                rawData = response?.ToString() ?? "데이터 없음";
        //            }

        //            if (eventName != "PING" && eventName != "PONG" && eventName != "ping" && eventName != "pong")
        //            {
        //                _logger.LogInformation($"📡 [CCTV 포착] '{eventName}' | 데이터: {rawData}");
        //            }
        //            return Task.CompletedTask;
        //        });

        //        client.OnDisconnected += (sender, e) =>
        //        {
        //            _logger.LogWarning($"❌ [연결 끊김] Socket.IO 연결 종료");
        //            _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //        };
        //        // [조명탄 3]
        //        _logger.LogWarning($"👉 [추적 3] {baseUri} 주소로 연결을 시도합니다... (5초 타임아웃 가동)");

        //        // 🚀 드디어 연결! 5초 안에 쇼부가 납니다.
        //        await client.ConnectAsync();

        //        // [조명탄 4]
        //        _logger.LogWarning("👉 [추적 4] Socket.IO 코드 통과 완료! 루프에 진입합니다.");

        //        _activeConnections[profile.ChzzkUid] = client;

        //        while (client.Connected && !stoppingToken.IsCancellationRequested)
        //        {
        //            await Task.Delay(1000, stoppingToken);
        //        }

        //        _logger.LogWarning($"❌ [연결 해제됨] {profile.ChzzkUid}의 루프를 종료합니다.");
        //        _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"❌ [치명적 에러] {ex.Message}");
        //        _activeConnections.TryRemove(profile.ChzzkUid, out _);
        //    }
        //}


        // ==========================================
        // 🧠 비즈니스 로직 (채팅 파싱 -> DB -> 오버레이)
        // ==========================================
        public async Task ProcessMessage(string chzzkUid, string message, int donationAmount = 0)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) return;

            bool isUpdated = false;
            string songCmd = profile.SongCommand ?? "!신청";

            if (!string.IsNullOrEmpty(message) && message.StartsWith(songCmd + " "))
            {
                int songPrice = profile.SongCheesePrice;
                if (songPrice == 0 || donationAmount >= songPrice)
                {
                    string content = message.Substring(songCmd.Length).Trim();
                    string title = content; string artist = "";
                    if (content.Contains("-"))
                    {
                        var parts = content.Split('-', 2);
                        title = parts[0].Trim(); artist = parts[1].Trim();
                    }

                    db.SongQueues.Add(new SongQueue
                    {
                        ChzzkUid = chzzkUid,
                        Title = title,
                        Artist = artist,
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        SortOrder = await db.SongQueues.CountAsync(s => s.ChzzkUid == chzzkUid && s.Status == "Pending")
                    });
                    isUpdated = true;
                    _logger.LogInformation($"[🔔 자동 신청 성공] {title} (스트리머: {chzzkUid})");
                    if (songPrice > 0) donationAmount -= songPrice;
                }
            }

            if (donationAmount > 0)
            {
                int price = profile.OmakaseCheesePrice > 0 ? profile.OmakaseCheesePrice : 1000;
                int addedOmakase = donationAmount / price;
                if (addedOmakase > 0)
                {
                    profile.OmakaseCount += addedOmakase;
                    isUpdated = true;
                    _logger.LogInformation($"[🍣 자동 오마카세 추가] {addedOmakase}곡 (스트리머: {chzzkUid})");
                }
            }

            if (isUpdated)
            {
                await db.SaveChangesAsync();
                await BroadcastUpdateToOverlay(db, chzzkUid);
            }
        }

        private async Task BroadcastUpdateToOverlay(AppDbContext db, string chzzkUid)
        {
            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            var songs = await db.SongQueues.Where(s => s.ChzzkUid == chzzkUid).OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt).ToListAsync();

            var state = new
            {
                memo = profile?.NoticeMemo ?? "",
                omakaseCount = profile?.OmakaseCount ?? 0,
                currentTitle = songs.FirstOrDefault(s => s.Status == "Playing")?.Title ?? "-",
                currentArtist = songs.FirstOrDefault(s => s.Status == "Playing")?.Artist ?? "",
                pendingSongs = songs.Where(s => s.Status == "Pending").ToList(),
                completedSongs = songs.Where(s => s.Status == "Completed").ToList(),
                labels = new { nowPlaying = "▶ NOW PLAYING", upNext = "⏳ UP NEXT", completed = "✔ COMPLETED" }
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonState = JsonSerializer.Serialize(state, options);
            await _hubContext.Clients.Group(chzzkUid).SendAsync("ReceiveOverlayState", jsonState);
            await _hubContext.Clients.Group(chzzkUid).SendAsync("RefreshDashboard");
        }
    }
}