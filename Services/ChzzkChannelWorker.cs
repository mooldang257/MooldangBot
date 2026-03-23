using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data; // DB 컨텍스트 네임스페이스
using MooldangAPI.Models; // 모델 네임스페이스
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;
using MooldangAPI.ApiClients;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangAPI.Services;

public class ChzzkChannelWorker
{
    private readonly string _uid;
    private readonly string _clientId;     // 매니저에게 받은 ID
    private readonly string _clientSecret; // 매니저에게 받은 Secret
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChzzkChannelWorker> _logger;

    // ⭐ [성능 개선] 공유 HttpClient (매 요청마다 new HttpClient() 생성하지 않음)
    private readonly HttpClient _httpClient;

    // ⭐ [성능 개선 #1] 명령어 캐시: DB 조회 없이 메모리에서 바로 검색
    private List<StreamerCommand>? _commandCache;
    private DateTime _commandCacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CommandCacheTtl = TimeSpan.FromSeconds(30);

    // ChzzkChannelWorker.cs 클래스 내부 전역 변수로 추가

    // 💡 [카테고리 사전]: 사용자가 입력하는 단축어 -> (타입, 치지직_카테고리_ID, 공식명칭)
    // 주의: "JustChatting", "LeagueOfLegends" 등의 ID 값은 실제 치지직 OpenAPI 규격에 맞는 고유 ID로 교체가 필요합니다.
    private static readonly Dictionary<string, string> CategorySearchAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        { "저챗", "talk" },
        { "소통", "talk" },
        { "노가리", "talk" },
        { "먹방", "먹방/쿡방" },
        { "노래", "음악/노래" },
        { "종겜", "종합 게임" },
    
        // 게임 줄임말
        { "롤", "리그 오브 레전드" },
        { "발로", "발로란트" },
        { "배그", "BATTLEGROUNDS" }, // 배틀그라운드
        { "마크", "Minecraft" },
        { "메", "메이플스토리" },
        { "로아", "로스트아크" },
        { "철권", "철권 8" }
    };

    //public ChzzkChannelWorker(string uid, IServiceProvider serviceProvider)
    //{
    //    _uid = uid;
    //    _serviceProvider = serviceProvider;
    //    // DI 컨테이너에서 로거를 직접 뽑아옵니다.
    //    _logger = serviceProvider.GetRequiredService<ILogger<ChzzkChannelWorker>>();
    //}

    // ⭐ 생성자에서 API 키를 받도록 수정
    public ChzzkChannelWorker(string uid, string clientId, string clientSecret, IServiceProvider serviceProvider)
    {
        _uid = uid;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ChzzkChannelWorker>>();

        // ⭐ [성능 개선 #4] 공유 HttpClient 초기화 (소켓 고갈 방지)
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Client-Id", _clientId);
        _httpClient.DefaultRequestHeaders.Add("Client-Secret", _clientSecret);
    }

    public async Task ConnectAndListenAsync(CancellationToken stoppingToken)
    {
        // 바깥쪽에 무한 루프를 씌워 소켓이 끊어지면 3초 뒤 다시 연결을 시도하도록 합니다.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. DB에서 스트리머 정보와 API 키를 안전하게 가져옵니다.
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                string currentUid = _uid;
                var profile = await dbContext.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == currentUid, stoppingToken);
                if (profile == null || !profile.IsBotEnabled || string.IsNullOrEmpty(profile.ChzzkAccessToken))
                {
                    _logger.LogWarning($"[물댕봇] {_uid}의 봇이 비활성화 상태이거나 액세스 토큰이 없어 연결을 대기합니다.");
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                // [추가된 부분] 방에 들어가기 전에 액세스 토큰 리프레시부터 검사합니다!
                await RefreshTokenIfNeededAsync(profile, _clientId, _clientSecret, dbContext);
                // ==========================================================

                // 2. 치지직 오픈 API에 세션 연결 요청 (HTTP)
                using var authClient = new HttpClient();

                // ⭐ 매니저가 넘겨준 키를 바로 사용!
                authClient.DefaultRequestHeaders.Add("Client-Id", _clientId);
                authClient.DefaultRequestHeaders.Add("Client-Secret", _clientSecret);
                authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

                var authRes = await authClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/sessions/auth", stoppingToken);
                var authJson = await authRes.Content.ReadAsStringAsync(stoppingToken);

                if (!authRes.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ [물댕봇] {_uid} 세션 발급 실패: {authJson}");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }
                using var authDoc = JsonDocument.Parse(authJson);
                string socketUrl = authDoc.RootElement.GetProperty("content").GetProperty("url").GetString() ?? "";

                // ⭐ [진범 검거] 완벽한 웹소켓 주소(문 + 열쇠)를 조립합니다.
                UriBuilder uriBuilder = new UriBuilder(socketUrl);
                uriBuilder.Scheme = "wss"; // https를 wss로 강제 변환

                // ⭐ [핵심] 치지직 Socket.IO 서버의 진짜 대문을 달아줍니다.
                if (uriBuilder.Path == "/")
                {
                    uriBuilder.Path = "/socket.io/";
                }

                // ⭐ 기존 인증키(auth) 뒤에 웹소켓 필수 옵션(EIO=3)을 쇠사슬처럼 묶습니다.
                string extraQuery = "transport=websocket&EIO=3";
                if (uriBuilder.Query.Length > 1) // 기존에 ?auth= 가 있다면
                {
                    uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + extraQuery;
                }
                else
                {
                    uriBuilder.Query = extraQuery;
                }

                string finalSocketUrl = uriBuilder.ToString();
                _logger.LogWarning($"📡 [물댕봇] 조립된 최종 URL: {finalSocketUrl}");

                // 3. 순정 웹소켓(ClientWebSocket) 연결
                using var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
                ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");

                await ws.ConnectAsync(new Uri(finalSocketUrl), stoppingToken);
                _logger.LogInformation($"✅ [물댕봇] {_uid} 물리적 연결 성공! 핸드셰이크를 시작합니다.");


                // 4. 데이터 수신 대기 루프 (매니저가 취소하기 전까지 무한 반복)
                var buffer = new byte[1024 * 16]; // 16KB 버퍼

                while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    string message = string.Empty;
                    try 
                    {
                        // ⭐ [타임아웃 안전장치] 60초간 응답 없으면 연결 끊기
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(180));

                        using var ms = new MemoryStream();
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCts.Token);
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage && !timeoutCts.Token.IsCancellationRequested);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        message = Encoding.UTF8.GetString(ms.ToArray());
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug($"⚠️ [물댕봇] {_uid} 채널 소켓 응답 지연(타임아웃 60초). 방송 상태를 재확인합니다.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"❌ [물댕봇] 수신 스트림 에러: {ex.Message}");
                        break;
                    }

                    try
                    {
                        // 🧠 [Socket.IO 프로토콜 수동 해석기]
                        if (message.StartsWith("0")) // 0: Open
                        {
                            _logger.LogInformation($"📦 [물댕봇] 서버 입장 수락. 방 입장(Connect)을 요청합니다.");
                            await SendMessageAsync(ws, "40", stoppingToken); // 40: Connect 패킷 발송
                        }
                        else if (message.StartsWith("2")) // 2: Ping
                        {
                            await SendMessageAsync(ws, "3", stoppingToken); // 3: Pong (살아있다고 대답)
                        }
                        else if (message.StartsWith("42")) // 42: Event (진짜 데이터)
                        {
                            await HandleEventAsync(message.Substring(2), profile, _clientId, _clientSecret, stoppingToken);
                        }
                    }
                    catch (Exception loopEx)
                    {
                        _logger.LogError($"❌ [물댕봇] 메시지 처리 중 지역 에러 (소켓유지): {loopEx.Message}\nRaw: {message.Substring(0, Math.Min(message.Length, 150))}");
                        // 루프를 깨지 않고 다음 메시지를 계속 기다립니다.
                    }
                }
                _logger.LogWarning($"⚠️ [물댕봇] {_uid} 웹소켓 연결이 종료되었습니다. 3초 후 재연결을 시도합니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [물댕봇] 소켓 통신 에러: {ex.Message}");
            }

            // 루프를 빠져나왔다 = 소켓이 끊어졌다. 
            // 서버에 무리를 주지 않기 위해 3초 대기 후 다시 바깥쪽 while 루프의 처음(재연결)으로 돌아갑니다.
            await Task.Delay(3000, stoppingToken);
        }
    }

    // ⭐ [업그레이드] 스마트 봇 토큰 발급/갱신 파이프라인 (커스텀 봇 우선 지원)
    private async Task<string?> GetAndRefreshBotTokenAsync(StreamerProfile profile, string clientId, string clientSecret, AppDbContext db)
    {
        // ==========================================
        // 🥇 1순위: 스트리머 커스텀 봇 계정 확인 및 갱신
        // ==========================================
        if (!string.IsNullOrEmpty(profile.BotRefreshToken))
        {
            // 1-1. 커스텀 봇 토큰이 아직 넉넉하게 살아있다면 바로 반환
            if (!string.IsNullOrEmpty(profile.BotAccessToken) &&
                profile.BotTokenExpiresAt.HasValue &&
                profile.BotTokenExpiresAt.Value > DateTime.Now.AddHours(1))
            {
                return profile.BotAccessToken;
            }

            _logger.LogWarning($"🔄 [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 만료 임박! 자동 갱신 시도...");

            using var httpClient = new HttpClient();
            var customTokenReq = new { grantType = "refresh_token", clientId, clientSecret, refreshToken = profile.BotRefreshToken };
            var customRes = await httpClient.PostAsync("https://openapi.chzzk.naver.com/auth/v1/token",
                new StringContent(JsonSerializer.Serialize(customTokenReq), Encoding.UTF8, "application/json"));

            if (customRes.IsSuccessStatusCode)
            {
                var json = await customRes.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement.GetProperty("content");

                // 스트리머 프로필에 커스텀 봇 새 토큰 저장
                profile.BotAccessToken = content.GetProperty("accessToken").GetString();
                profile.BotRefreshToken = content.GetProperty("refreshToken").GetString() ?? profile.BotRefreshToken;
                profile.BotTokenExpiresAt = DateTime.Now.AddSeconds(content.GetProperty("expiresIn").GetInt32());

                await db.SaveChangesAsync();
                _logger.LogInformation($"✅ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 성공!");
                return profile.BotAccessToken;
            }
            else
            {
                _logger.LogError($"❌ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 실패! 기본 봇으로 폴백(Fallback)합니다.");
                // 실패하더라도 2순위(공통 봇)로 넘어가도록 return 하지 않고 계속 진행합니다.
            }
        }

            // ==========================================
            // 🥈 2순위: 시스템 공통 봇 계정 확인 및 갱신 (SystemSettings)
            // ==========================================
            var botKeys = new[] { "BotAccessToken", "BotRefreshToken", "BotTokenExpiresAt" };
            var globalSettings = await db.SystemSettings.Where(s => botKeys.Contains(s.KeyName)).ToListAsync();
            var globalTokenSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotAccessToken");
            var globalRefreshSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotRefreshToken");
            var globalExpiresSetting = globalSettings.FirstOrDefault(s => s.KeyName == "BotTokenExpiresAt");

            string? globalToken = globalTokenSetting?.KeyValue;
            string? globalRefresh = globalRefreshSetting?.KeyValue;
            DateTime globalExpireDate = DateTime.MinValue;

            if (globalExpiresSetting != null && DateTime.TryParse(globalExpiresSetting.KeyValue, out var parsedDate))
            {
                globalExpireDate = parsedDate;
            }

            if (!string.IsNullOrEmpty(globalToken) && globalExpireDate > DateTime.Now.AddHours(1))
            {
                return globalToken;
            }

            if (!string.IsNullOrEmpty(globalRefresh))
            {
                _logger.LogWarning("🔄 [물댕봇] 시스템 공통 봇 토큰 만료 임박! 자동 갱신 시도...");

                using var httpClient = new HttpClient();
                var globalTokenReq = new { grantType = "refresh_token", clientId, clientSecret, refreshToken = globalRefresh };
                var globalRes = await httpClient.PostAsync("https://openapi.chzzk.naver.com/auth/v1/token",
                    new StringContent(JsonSerializer.Serialize(globalTokenReq), Encoding.UTF8, "application/json"));

                if (globalRes.IsSuccessStatusCode)
                {
                    var json = await globalRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var content = doc.RootElement.GetProperty("content");

                    string newAccess = content.GetProperty("accessToken").GetString() ?? "";
                    string newRefresh = content.GetProperty("refreshToken").GetString() ?? globalRefresh;
                    DateTime newExpire = DateTime.Now.AddSeconds(content.GetProperty("expiresIn").GetInt32());

                    UpdateOrAddSystemSetting(db, "BotAccessToken", newAccess);
                    UpdateOrAddSystemSetting(db, "BotRefreshToken", newRefresh);
                    UpdateOrAddSystemSetting(db, "BotTokenExpiresAt", newExpire.ToString("O"));

                    await db.SaveChangesAsync();
                    _logger.LogInformation("✅ [물댕봇] 시스템 공통 봇 토큰 갱신 성공!");
                    return newAccess;
                }
                else
                {
                    _logger.LogError("❌ [물댕봇] 시스템 공통 봇 갱신마저 실패했습니다!");
                }
            }

            // ==========================================
            // 🥉 3순위: 최후의 수단 (스트리머 본인 토큰 반환)
            // ==========================================
            _logger.LogWarning($"⚠️ [물댕봇] 사용 가능한 봇 토큰이 없어 {profile.ChzzkUid}님의 방송용 계정으로 채팅을 보냅니다.");
            return profile.ChzzkAccessToken;
        }

        // 헬퍼 메서드 (동일)
        private void UpdateOrAddSystemSetting(AppDbContext db, string key, string value)
        {
            var setting = db.SystemSettings.Local.FirstOrDefault(s => s.KeyName == key)
                       ?? db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
            if (setting == null) db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
            else setting.KeyValue = value;
        }

    // ⭐ [성능 개선 #1] 명령어 캐시 갱신 메서드
    private async Task RefreshCommandCacheAsync(string chzzkUid, CancellationToken token)
    {
        if (DateTime.Now < _commandCacheExpiry && _commandCache != null) return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _commandCache = await db.StreamerCommands
            .AsNoTracking()
            .Where(c => c.ChzzkUid == chzzkUid)
            .ToListAsync(token);
        _commandCacheExpiry = DateTime.Now.Add(CommandCacheTtl);
    }

    // ⭐ [비밀 무기] 토큰 유통기한 확인 및 자동 갱신 메서드
    private async Task RefreshTokenIfNeededAsync(StreamerProfile profile, string clientId, string clientSecret, AppDbContext db)
    {
        // 1. 만료 시간(TokenExpiresAt)이 1시간 이상 넉넉하게 남았다면 그냥 통과!
        if (profile.TokenExpiresAt.HasValue && profile.TokenExpiresAt.Value > DateTime.Now.AddHours(1))
        {
            return;
        }

        _logger.LogWarning($"🔄 [물댕봇] {profile.ChzzkUid}의 액세스 토큰 만료가 임박했습니다. 자동 갱신을 시도합니다...");

        using var httpClient = new HttpClient();

        // 치지직 토큰 갱신 규격에 맞춰 요청서 작성
        var tokenRequest = new
        {
            grantType = "refresh_token", // "나 리프레시 토큰 쓸래!"
            clientId = clientId,
            clientSecret = clientSecret,
            refreshToken = profile.ChzzkRefreshToken // DB에 있던 리프레시 토큰 제출
        };

        var content = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://openapi.chzzk.naver.com/auth/v1/token", content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResult = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResult);
            var tokenContent = doc.RootElement.GetProperty("content");

            // ⭐ 2. 새로 발급받은 따끈따끈한 토큰으로 DB 정보 업데이트
            profile.ChzzkAccessToken = tokenContent.GetProperty("accessToken").GetString() ?? "";
            profile.ChzzkRefreshToken = tokenContent.GetProperty("refreshToken").GetString() ?? profile.ChzzkRefreshToken; // 리프레시 토큰도 새로 나올 수 있음
            profile.TokenExpiresAt = DateTime.Now.AddSeconds(tokenContent.GetProperty("expiresIn").GetInt32());

            await db.SaveChangesAsync(); // DB에 영구 저장
            _logger.LogInformation($"✅ [물댕봇] {profile.ChzzkUid} 토큰 자동 재발급 및 DB 업데이트 완료! (수명 연장)");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"❌ [물댕봇] {profile.ChzzkUid} 토큰 갱신 실패! 스트리머의 재로그인이 필요할 수 있습니다. 사유: {error}");
        }
    }

    // 데이터 처리기
    private async Task HandleEventAsync(string jsonArray, StreamerProfile profile, string clientId, string clientSecret, CancellationToken token)
    {
        try
        {
            // ⭐ [추가] 이벤트 처리 전 토큰 만료 여부를 상시 체크하여 갱신합니다.
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await RefreshTokenIfNeededAsync(profile, clientId, clientSecret, db);
            }

            using var doc = JsonDocument.Parse(jsonArray);
            string eventName = doc.RootElement[0].GetString() ?? "";

            // 📡 [분석용 로그 추가] 어떤 이벤트가 들어오는지 실시간으로 확인합니다.
            _logger.LogInformation($"📡 [ChzzkWorker] 이벤트 수신: {eventName}");
            
            if (eventName != "CHAT" && eventName != "SYSTEM" && eventName != "DONATION")
            {
                _logger.LogDebug($"📡 [이벤트 분석] 기타 이벤트: {eventName}");
            }

            // ⭐ [핵심 진범 검거] 두 번째 데이터는 JSON 문자열이므로, 한 번 더 Parse 해야 합니다!
            string payloadString = doc.RootElement[1].GetString() ?? "{}";
            using var payloadDoc = JsonDocument.Parse(payloadString);
            var payload = payloadDoc.RootElement;

            if (eventName == "error")
            {
                _logger.LogDebug($"📡 [ChzzkWorker] 소켓 IO 에러 수신: {payloadString}");
                return;
            }

            if (eventName == "SYSTEM")
            {
                if (payload.GetProperty("type").GetString() == "connected")
                {
                    string sessionKey = payload.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
                    _logger.LogInformation($"💎 [Session Key 획득 성공!] {sessionKey}");

                    // 채팅 구독권 신청
                    using var subClient = new HttpClient();
                    subClient.DefaultRequestHeaders.Add("Client-Id", clientId);
                    subClient.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
                    subClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

                    var subReq = new { channelId = profile.ChzzkUid };
                    
                    // 1. 채팅 구독
                    var chatSubRes = await subClient.PostAsync($"https://openapi.chzzk.naver.com/open/v1/sessions/events/subscribe/chat?sessionKey={sessionKey}",
                        new StringContent(JsonSerializer.Serialize(subReq), Encoding.UTF8, "application/json"), token);

                    if (chatSubRes.IsSuccessStatusCode)
                        _logger.LogInformation($"🎉 [물댕봇] {_uid} 채팅 이벤트 구독 완료!");
                    else
                        _logger.LogError($"❌ [채팅 구독 실패] {await chatSubRes.Content.ReadAsStringAsync()}");

                    // 2. 후원(Donation) 구독
                    var donSubRes = await subClient.PostAsync($"https://openapi.chzzk.naver.com/open/v1/sessions/events/subscribe/donation?sessionKey={sessionKey}",
                        new StringContent(JsonSerializer.Serialize(subReq), Encoding.UTF8, "application/json"), token);

                    if (donSubRes.IsSuccessStatusCode)
                        _logger.LogInformation($"💰 [물댕봇] {_uid} 후원 이벤트 구독 완료! 이제 진짜 치즈 후원을 인식합니다.");
                    else
                        _logger.LogError($"❌ [후원 구독 실패] {await donSubRes.Content.ReadAsStringAsync()}");
                }
            }
            else if (eventName == "DONATION")
            {
                try
                {
                    // [디버깅] 실제 DONATION 페이로드가 어떻게 생겼는지 분석하기 위해 전체 출력
                    _logger.LogInformation($"[DONATION 원본 데이터]: {payloadString}");

                    string nickname = "익명의 후원자";
                    if (payload.TryGetProperty("donatorNickname", out var dNickProp))
                    {
                        nickname = dNickProp.GetString() ?? nickname;
                    }
                    else if (payload.TryGetProperty("profile", out var profileProp))
                    {
                        string profileJson = profileProp.ValueKind == JsonValueKind.String 
                            ? profileProp.GetString() ?? "{}" 
                            : profileProp.GetRawText();
                        using var profileDoc = JsonDocument.Parse(profileJson);
                        if (profileDoc.RootElement.TryGetProperty("nickname", out var nickProp))
                        {
                            nickname = nickProp.GetString() ?? nickname;
                        }
                    }

                    string senderId = payload.TryGetProperty("donatorChannelId", out var dcIdProp) ? dcIdProp.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(senderId)) senderId = payload.TryGetProperty("userChannelId", out var uIdProp) ? uIdProp.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(senderId)) senderId = payload.TryGetProperty("channelId", out var idProp) ? idProp.GetString() ?? "" : "";

                    int payAmount = 0;
                    if (payload.TryGetProperty("payAmount", out var payProp))
                    {
                        if (payProp.ValueKind == JsonValueKind.Number) payAmount = payProp.GetInt32();
                        else if (payProp.ValueKind == JsonValueKind.String && int.TryParse(payProp.GetString(), out int parsedPay)) payAmount = parsedPay;
                    }
                    else if (payload.TryGetProperty("donation", out var donProp) && donProp.TryGetProperty("payAmount", out var dpProp))
                    {
                        if (dpProp.ValueKind == JsonValueKind.Number) payAmount = dpProp.GetInt32();
                        else if (dpProp.ValueKind == JsonValueKind.String && int.TryParse(dpProp.GetString(), out int parsedPay)) payAmount = parsedPay;
                    }

                    string message = "";
                    if (payload.TryGetProperty("donationText", out var dTextProp)) message = dTextProp.GetString() ?? "";
                    if (string.IsNullOrEmpty(message) && payload.TryGetProperty("content", out var contentProp)) message = contentProp.GetString() ?? "";
                    if (string.IsNullOrEmpty(message) && payload.TryGetProperty("comment", out var commentProp)) message = commentProp.GetString() ?? "";

                    // [추가] 이모티콘 맵 추출
                    var emojis = new Dictionary<string, string>();
                    if (payload.TryGetProperty("emojis", out var emojiProp) && emojiProp.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in emojiProp.EnumerateObject())
                        {
                            string url = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(url)) emojis[prop.Name] = url;
                        }
                    }

                    _logger.LogInformation($"💰 [후원 수신] {nickname}님: {payAmount}원 / 메시지: {message} / 이모티콘 {emojis.Count}개");

                    if (payAmount > 0)
                    {
                        using var mediatorScope = _serviceProvider.CreateScope();
                        var mediator = mediatorScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                        await mediator.Publish(new MooldangAPI.Features.Chat.Events.ChatMessageReceivedEvent(
                            profile, nickname, message, "common_user", senderId, clientId, clientSecret, emojis, payAmount), token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ [DONATION 파싱 실패] {ex.Message} \n Raw: {payloadString}");
                }
            }
            else if (eventName == "SUBSCRIPTION")
            {
                // 정기 구독 이벤트 처리
                string profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                        ? payload.GetProperty("profile").GetString() ?? "{}"
                                        : payload.GetProperty("profile").GetRawText();
                using var profileDoc = JsonDocument.Parse(profileJson);
                string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var nickProp) ? nickProp.GetString() ?? "시청자" : "시청자";
                
                _logger.LogInformation($"⭐ [구독 이벤트 수신] {nickname}님 구독 중!");
            }
            else if (eventName == "CHAT")
            {
                // CHAT 이벤트도 동일하게 이중 포장되어 들어옵니다.
                string msg = payload.GetProperty("content").GetString() ?? "";

                // profile 정보는 그 안에 또 문자열로 들어있을 수 있으므로 안전하게 처리
                string profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                        ? payload.GetProperty("profile").GetString() ?? "{}"
                                        : payload.GetProperty("profile").GetRawText();

                using var profileDoc = JsonDocument.Parse(profileJson);
                string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var nickProp) ? nickProp.GetString() ?? "시청자" : "시청자";

                // ⭐ [보안 추가] 채팅 친 사람의 권한 확인 ("streamer", "manager", "common_user" 등)
                string userRole = profileDoc.RootElement.TryGetProperty("userRoleCode", out var roleProp) ? roleProp.GetString() ?? "common_user" : "common_user";

                // ⭐ [마스터 키 추가] 채팅을 보낸 사람의 고유 ID 추출
                string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";

                // ⭐ [슈퍼 유저 여부 확인] mooldang님의 고유 ID를 마스터로 지정하는 대신, 스트리머 본인이면 무조건 마스터로 인정합니다.
                bool isMaster = senderId == profile.ChzzkUid || senderId == "ca98875d5e0edf02776047fbc70f5449";
                
                //⭐ [봇계정 여부 확인] 봇 계정의 고유 ID를 봇으로 지정합니다. (시스템 설정에 등록된 봇 ID가 있다면 그것도 연동 필요)
                bool isBot = senderId == "445df9c493713244a65d97e4fd1ed0b1";

                _logger.LogInformation($"💬 [{nickname}({userRole})]: {msg} (IsMaster: {isMaster})");

                // ⭐ [후원 금액] extras 파싱 (payAmount 추출)
                int donationAmount = 0;
                if (payload.TryGetProperty("extras", out var extrasProp) && extrasProp.ValueKind == JsonValueKind.String)
                {
                    try {
                        string extrasJson = extrasProp.GetString() ?? "{}";
                        using var extrasDoc = JsonDocument.Parse(extrasJson);
                        if (extrasDoc.RootElement.TryGetProperty("payAmount", out var payProp))
                        {
                            donationAmount = payProp.GetInt32();
                            _logger.LogInformation($"💰 [후원 발생] {nickname}님: {donationAmount}원");
                        }
                    } catch {}
                }

                // ⭐ [이모티콘] emojis 파싱 (보안상 안전하게 dictionary 추출)
                var emojisDict = new Dictionary<string, string>();
                if (payload.TryGetProperty("emojis", out var emojisProp) && emojisProp.ValueKind == JsonValueKind.Object)
                {
                    try {
                        emojisDict = JsonSerializer.Deserialize<Dictionary<string, string>>(emojisProp.GetRawText()) ?? new Dictionary<string, string>();
                    } catch {}
                }

                // 명령어 처리는 이제 CustomCommandEventHandler (MediatR)에서 통합 관리합니다.
                // 🌟 [이벤트 발송] 중계기를 통해 모든 핸들러가 채팅을 받을 수 있도록 합니다.
                using var mediatorScope = _serviceProvider.CreateScope();
                var mediator = mediatorScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                await mediator.Publish(new MooldangAPI.Features.Chat.Events.ChatMessageReceivedEvent(
                    profile, nickname, msg, userRole, senderId, _clientId, _clientSecret, emojisDict, donationAmount), token);

                // 명령어 처리 (DB에서 설정한 값 사용)
                string songCmd = profile.SongCommand ?? "!신청";
                string omaCmd = profile.OmakaseCommand ?? "!물마카세";


            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"[패킷 무시] 파싱 에러: {ex.Message} \n원본: {jsonArray}");
        }
    }


    // 서버로 메시지(패킷)를 전송하는 도우미
    private async Task SendMessageAsync(ClientWebSocket ws, string msg, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(msg);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
    }

    // ChzzkChannelWorker.cs 하단에 추가 (SendMessageAsync 메서드 아래 등)

    // ChzzkChannelWorker.cs

    // 편의를 위한 봇 채팅 응답 헬퍼 메서드
    // ⭐ [성능 개선 #4] 공유 HttpClient 사용
    private async Task SendReplyChatAsync(StreamerProfile profile, string clientId, string clientSecret, string message, CancellationToken token)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            string tokenToUse = await GetAndRefreshBotTokenAsync(profile, clientId, clientSecret, db);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-Id", clientId);
            client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);

            var replyReq = new { message = "\u200B" + message }; // 중복 방지 접두사 강제 포함
            var res = await client.PostAsync("https://openapi.chzzk.naver.com/open/v1/chats/send",
                new StringContent(JsonSerializer.Serialize(replyReq), Encoding.UTF8, "application/json"), token);
            
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError($"❌ [채팅 발송 실패] {await res.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [SendReplyChatAsync 에러] {ex.Message}");
        }
    }

}