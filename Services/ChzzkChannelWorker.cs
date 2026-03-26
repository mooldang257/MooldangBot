using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence; // DB 컨텍스트 네임스페이스
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs; // 모델 네임스페이스
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;
using MooldangAPI.ApiClients;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangAPI.Services;

public class ChzzkChannelWorker
{
    private readonly ILogger<ChzzkChannelWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _uid;
    // ⭐ [성능 개선 #4] 중앙화된 API 클라이언트 사용 (하드코딩된 HttpClient 제거)
    private readonly ChzzkApiClient _chzzkApi;

    // ⭐ [성능 개선 #1] 명령어 캐시 서비스 주입
    private readonly ICommandCacheService _cacheService;

    // 💡 [무중단 안정성 #1] Scoped DB 컨텍스트 관리를 위한 팩토리 주입
    private readonly IServiceScopeFactory _scopeFactory;

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
    // ⭐ 생성자에서 ChzzkApiClient를 주입받도록 수정
    public ChzzkChannelWorker(string uid, IServiceProvider serviceProvider, ChzzkApiClient chzzkApi)
    {
        _uid = uid;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ChzzkChannelWorker>>();
        _cacheService = serviceProvider.GetRequiredService<ICommandCacheService>();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _chzzkApi = chzzkApi;
    }

    public async Task ConnectAndListenAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
            ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            try
            {
                // 1. 스트리머 정보 및 토큰 준비
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var profile = await dbContext.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == _uid, stoppingToken);
                if (profile == null || !profile.IsBotEnabled || string.IsNullOrEmpty(profile.ChzzkAccessToken))
                {
                    _logger.LogWarning($"[물댕봇] {_uid} 봇 비활성화 또는 토큰 없음. 10초 대기...");
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                await RefreshTokenIfNeededAsync(profile, dbContext);
                await _cacheService.RefreshAsync(_uid, stoppingToken);

                // 2. 세션 인증 및 소켓 URL 조립
                var sessionAuth = await _chzzkApi.GetSessionAuthAsync(profile.ChzzkAccessToken!);
                if (sessionAuth == null)
                {
                    _logger.LogError($"❌ [물댕봇] {_uid} 세션 발급 실패. 5초 대기...");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                string socketUrl = sessionAuth.Content?.Url ?? "";
                UriBuilder uriBuilder = new UriBuilder(socketUrl) { Scheme = "wss" };
                if (uriBuilder.Path == "/") uriBuilder.Path = "/socket.io/";
                
                string extraQuery = "transport=websocket&EIO=3";
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                    ? extraQuery 
                    : uriBuilder.Query.Substring(1) + "&" + extraQuery;

                string finalSocketUrl = uriBuilder.ToString();
                _logger.LogWarning($"📡 [물댕봇] 조립된 최종 URL: {finalSocketUrl}");

                // 3. 소켓 연결
                await ws.ConnectAsync(new Uri(finalSocketUrl), stoppingToken);
                _logger.LogInformation($"✅ [물댕봇] {_uid} 물리적 연결 성공! 무중단 병렬 루프를 시작합니다.");

                // 4. [핵심] 무중단 병렬 루프 (수신 vs 심장박동)
                using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                
                var receiveTask = ReceiveLoopAsync(ws, profile, loopCts.Token);
                var pingTask = PingLoopAsync(ws, loopCts.Token);

                // 둘 중 하나라도 예외가 발생하거나 종료되면 즉각 리셋 및 재시작
                var completedTask = await Task.WhenAny(receiveTask, pingTask);
                
                if (completedTask.IsFaulted)
                {
                    _logger.LogError(completedTask.Exception, "❌ [무중단] 루프 내 작업 중 심각한 오류 발생!");
                }
                
                loopCts.Cancel(); // 나머지 작업 강제 종료
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [물댕봇] 소켓 통신/연결 에러. 즉각 재연결을 시도합니다.");
            }
            finally
            {
                if (ws.State == WebSocketState.Open)
                {
                    try { await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None); } catch { }
                }
                
                _logger.LogWarning("⚠️ [물댕봇] 소켓 연결이 해제되었습니다. 0.5초 후 복구를 시작합니다.");
                if (!stoppingToken.IsCancellationRequested) await Task.Delay(500, stoppingToken);
            }
        }
    }

    // 🩺 [심장 박동] 치지직 서버에 10초마다 생존 신고(Ping)
    private async Task PingLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var pingMessage = Encoding.UTF8.GetBytes("2");
        var buffer = new ArraySegment<byte>(pingMessage);

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
                    _logger.LogDebug($"📡 [심장박동] {_uid} Ping 완료");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [심장박동] {_uid} 전송 중 이상 감지: {ex.Message}");
                throw; // Task.WhenAny에서 감지하도록 던짐
            }
        }
    }

    // 📨 [메시지 수집] 16KB 대용량 버퍼로 메시지 수집
    private async Task ReceiveLoopAsync(ClientWebSocket ws, StreamerProfile profile, CancellationToken ct)
    {
        var buffer = new byte[1024 * 16]; // 16KB 확장
        using var ms = new MemoryStream();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning($"⚠️ [Receive] {_uid} 서버에서 Close 요청 수신.");
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(ms.ToArray());
                ms.SetLength(0);

                if (string.IsNullOrEmpty(message)) continue;

                // Socket.IO 프로토콜 해석 및 분기
                if (message.StartsWith("0")) // Open
                {
                    _logger.LogInformation("📦 [Receive] 서버 입장 수락. 방 입장 요청(40)");
                    await SendMessageAsync(ws, "40", ct);
                }
                else if (message.StartsWith("2")) // Ping 수신 시 Pong(3) 즉각 대응
                {
                    await SendMessageAsync(ws, "3", ct);
                }
                else if (message.StartsWith("42")) // Event (메시지/후원 등)
                {
                    // 수신 루프가 막히지 않도록 비동기로 처리하되, 전용 Scope 부여
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        try 
                        {
                            await HandleEventAsync(message.Substring(2), profile, scope, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ [HandleEvent] 비동기 처리 중 에러 발생");
                        }
                    }, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [Receive] {_uid} 수신 스트림 오류: {ex.Message}");
                throw; // Task.WhenAny에서 감지하도록 던짐
            }
        }
    }

    // ⭐ [업그레이드] 스마트 봇 토큰 발급/갱신 파이프라인 (커스텀 봇 우선 지원)
    private async Task<string?> GetAndRefreshBotTokenAsync(StreamerProfile profile, AppDbContext db)
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

            var customTokenRes = await _chzzkApi.RefreshTokenAsync(profile.BotRefreshToken);

            if (customTokenRes != null && customTokenRes.Code == 200 && customTokenRes.Content != null)
            {
                var content = customTokenRes.Content;
                profile.BotAccessToken = content.AccessToken;
                profile.BotRefreshToken = content.RefreshToken ?? profile.BotRefreshToken;
                profile.BotTokenExpiresAt = DateTime.Now.AddSeconds(content.ExpiresIn);

                await db.SaveChangesAsync();
                _logger.LogInformation($"✅ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 성공!");
                return profile.BotAccessToken;
            }
            else
            {
                _logger.LogError($"❌ [물댕봇] {profile.ChzzkUid}님의 커스텀 봇 토큰 갱신 실패! 기본 봇으로 폴백(Fallback)합니다.");
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

                var globalTokenRes = await _chzzkApi.RefreshTokenAsync(globalRefresh);

                if (globalTokenRes != null && globalTokenRes.Code == 200 && globalTokenRes.Content != null)
                {
                    var content = globalTokenRes.Content;
                    string newAccess = content.AccessToken ?? "";
                    string newRefresh = content.RefreshToken ?? globalRefresh;
                    DateTime newExpire = DateTime.Now.AddSeconds(content.ExpiresIn);

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


    // ⭐ [비밀 무기] 토큰 유통기한 확인 및 자동 갱신 메서드
    private async Task RefreshTokenIfNeededAsync(StreamerProfile profile, AppDbContext db)
    {
        // 1. 만료 시간(TokenExpiresAt)이 1시간 이상 넉넉하게 남았다면 그냥 통과!
        if (profile.TokenExpiresAt.HasValue && profile.TokenExpiresAt.Value > DateTime.Now.AddHours(1))
        {
            return;
        }

        _logger.LogWarning($"🔄 [물댕봇] {profile.ChzzkUid}의 액세스 토큰 만료가 임박했습니다. 자동 갱신을 시도합니다...");

        var tokenRes = await _chzzkApi.RefreshTokenAsync(profile.ChzzkRefreshToken ?? "");

        if (tokenRes != null && tokenRes.Code == 200 && tokenRes.Content != null)
        {
            var content = tokenRes.Content;
            // ⭐ 2. 새로 발급받은 따끈따끈한 토큰으로 DB 정보 업데이트
            profile.ChzzkAccessToken = content.AccessToken ?? "";
            profile.ChzzkRefreshToken = content.RefreshToken ?? profile.ChzzkRefreshToken; // 리프레시 토큰도 새로 나올 수 있음
            profile.TokenExpiresAt = DateTime.Now.AddSeconds(content.ExpiresIn);

            await db.SaveChangesAsync(); // DB에 영구 저장
            _logger.LogInformation($"✅ [물댕봇] {profile.ChzzkUid} 토큰 자동 재발급 및 DB 업데이트 완료! (수명 연장)");
        }
        else
        {
            _logger.LogError($"❌ [물댕봇] {profile.ChzzkUid} 토큰 갱신 실패! 스트리머의 재로그인이 필요할 수 있습니다.");
        }
    }

    // 데이터 처리기
    private async Task HandleEventAsync(string jsonArray, StreamerProfile profile, IServiceScope scope, CancellationToken token)
    {
        try
        {

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
                _logger.LogWarning($"📡 [ChzzkWorker] {_uid} 소켓 IO 에러 수신: {payloadString}");
                return;
            }

            if (eventName == "SYSTEM")
            {
                if (payload.GetProperty("type").GetString() == "connected")
                {
                    string sessionKey = payload.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
                    _logger.LogInformation($"💎 [Session Key 획득 성공!] {sessionKey}");

                    // 채팅 구독권 신청
                    // [성능 개선 #4] ChzzkApiClient를 통해 이벤트 구독 신청
                    bool chatSubscribed = await _chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "chat", profile.ChzzkUid);
                    if (chatSubscribed)
                        _logger.LogInformation($"🎉 [물댕봇] {_uid} 채팅 이벤트 구독 완료!");
                    else
                        _logger.LogError($"❌ [채팅 구독 실패]");

                    bool donSubscribed = await _chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "donation", profile.ChzzkUid);
                    if (donSubscribed)
                        _logger.LogInformation($"💰 [물댕봇] {_uid} 후원 이벤트 구독 완료!");
                    else
                        _logger.LogError($"❌ [후원 구독 실패]");
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
                        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                        await mediator.Publish(new MooldangAPI.Features.Chat.Events.ChatMessageReceivedEvent(
                            profile, nickname, message, "common_user", senderId, emojis, payAmount), token);
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

                // ⭐ [슈퍼 유저 여부 확인] 스트리머 본인이거나 관리 코드를 가진 경우 마스터로 인정
                bool isMaster = senderId.Equals(profile.ChzzkUid, StringComparison.OrdinalIgnoreCase) || 
                                senderId.Equals("ca98875d5e0edf02776047fbc70f5449", StringComparison.OrdinalIgnoreCase);
                
                //⭐ [봇계정 여부 확인] 봇 계정의 고유 ID를 봇으로 지정합니다.
                bool isBot = senderId.Equals("445df9c493713244a65d97e4fd1ed0b1", StringComparison.OrdinalIgnoreCase);

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
                var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                await mediator.Publish(new MooldangAPI.Features.Chat.Events.ChatMessageReceivedEvent(
                    profile, nickname, msg, userRole, senderId, emojisDict, donationAmount), token);

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
    // ⭐ [성능 개선 #4] ChzzkApiClient 사용
    private async Task SendReplyChatAsync(StreamerProfile profile, string message, CancellationToken token)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            string? tokenToUse = await GetAndRefreshBotTokenAsync(profile, db);

            if (string.IsNullOrEmpty(tokenToUse)) return;

            bool sent = await _chzzkApi.SendChatMessageAsync(tokenToUse, message);
            
            if (!sent)
            {
                _logger.LogError($"❌ [채팅 발송 실패] {profile.ChzzkUid}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [SendReplyChatAsync 에러] {ex.Message}");
        }
    }

}