[IMPL] Chzzk 인증 정보 전달 체계 개선 (ID 미스매치 해결)
이 계획은 스트리머 개별 API 정보를 소켓 연결 단계까지 안전하게 전달하여, auth fail 무한 루프를 근본적으로 차단하는 것을 목표로 합니다.

User Review Required
IMPORTANT

이 작업은 IChzzkApiClient의 인터페이스 변경을 포함하므로, 이를 구현하는 모든 클래스에 영향을 미칩니다. (현재 ChzzkApiClient 및 테스트 목용 클래스 등)

Proposed Changes
1. API 클라이언트 레이어 개방
[MODIFY] 

IChzzkApiClient.cs
세션 인증 정보를 가져올 때 외부에서 API 정보를 주입할 수 있도록 인터페이스를 확장합니다.

csharp
// 기존
Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken);
// 변경 (clientId, clientSecret 추가)
Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null);
[MODIFY] 

ChzzkApiClient.cs
인자가 있으면 사용하고, 없으면 전역 설정을 사용하도록 수정합니다.

csharp
public async Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken, string? clientId = null, string? clientSecret = null)
{
    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/open/v1/sessions/auth");
        // 핵심: 외부 주입 ID가 있으면 우선 사용, 없으면 전역 ID(_clientId) 사용
        request.Headers.Add("Client-Id", clientId ?? _clientId);
        request.Headers.Add("Client-Secret", clientSecret ?? _clientSecret);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        // ... (이하 동일)
    }
    catch { return null; }
}
2. 소켓 샤딩 및 서비스 레이어 전파
[MODIFY] 

WebSocketShard.cs
ConnectAsync가 토큰 외에 ID 정보도 함께 받도록 수정합니다.

csharp
public async Task<bool> ConnectAsync(string chzzkUid, string accessToken, string? clientId = null, string? clientSecret = null)
{
    // ...
    // 세션 인증 호출 시 전달받은 ID 정보 전달
    var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken, clientId, clientSecret);
    // ...
}
[MODIFY] 

ChzzkBotService.cs
DB에서 읽어온 StreamerProfile의 개별 앱 정보를 소켓에 주입합니다.

csharp
private async Task ConnectInternalAsync(string chzzkUid, bool forceFresh)
{
    // ... profile 로드 로직 ...
    
    // 스트리머 개인 앱 정보 추출
    string? cId = profile.ApiClientId;
    string? cSec = profile.ApiClientSecret;
    // 소켓 연결 시 해당 정보들과 함께 호출
    bool success = await _chatClient.ConnectAsync(chzzkUid, accessToken, cId, cSec);
    // ...
}
Open Questions
WARNING

현재 진행 중인 암호화 작업이 완료되면 profile.ApiClientId 등은 복호화된 상태로 전달되어야 합니다. 암호화 로직이 적용된 Converter가 이 시점에 이미 작동 중인지 확인이 필요합니다.

Verification Plan
Automated Tests
TokenRenewalService가 개인 앱 정보로 토큰 갱신에 성공하는지 확인.
HandleAuthFailureAsync가 호출되었을 때, 로그에 찍히는 Client-Id가 전역 설정이 아닌 스트리머 개인의 것인지 확인.
소켓 연결 후 auth fail 없이 connected 메시지가 정상 수신되는지 확인.
이 계획대로 진행하면 암호화 로직과는 별개로 기존의 고질적인 접속 문제를 해결할 수 있습니다. 승인해 주시면 즉시 코드 수정에 착수하겠습니다. (물론 암호화 작업 중인 파일들과의 충돌을 최소화하며 진행하겠습니다!)