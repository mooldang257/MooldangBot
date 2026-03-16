using MooldangAPI.ApiClients;
using MooldangAPI.Models;
using System.Net.Http.Headers;
using System.Text.Json; // 상단에 추가되어 있는지 확인

namespace MooldangAPI.ApiClients
{
    public class ChzzkApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://openapi.chzzk.naver.com"; // 치지직 공식 Open API 주소

        public ChzzkApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            // 통신 객체가 생성될 때, 수호자로부터 해독된 키를 받아 헤더에 주입합니다.
            // 치지직 Open API 인증 표준 헤더 규격 (Client-Id, Client-Secret)
            string clientId = SecretGuardian.GetClientId();
            string clientSecret = SecretGuardian.GetClientSecret();

            _httpClient.DefaultRequestHeaders.Add("Client-Id", clientId);
            _httpClient.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        }

        /// <summary>
        /// [파로스의 자각]: 버튜버/스트리머의 채널 정보를 조회합니다.
        /// </summary>
        public async Task<string> GetChannelInfoAsync(string channelId)
        {
            try
            {
                // 인증 헤더가 포함된 상태로 채널 정보 요청
                var response = await _httpClient.GetAsync($"/open/v1/channels/{channelId}");

                // 오시리스의 심판: 인증 실패(401) 등 오류 시 예외 발생
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                return $"[하모니 경고] 인증 또는 통신 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// [텔로스5의 연성]: 일회성 인증 코드(code)를 영구적인 Access Token으로 교환합니다.
        /// </summary>
        public async Task<string?> ExchangeCodeForTokenAsync(string code, string? state)
        {
            try
            {
                string clientId = SecretGuardian.GetClientId();
                string clientSecret = SecretGuardian.GetClientSecret();

                var requestData = new
                {
                    grantType = "authorization_code",
                    clientId = clientId,
                    clientSecret = clientSecret,
                    code = code,
                    state = state ?? string.Empty
                };

                var content = JsonContent.Create(requestData);
                string tokenEndpoint = "https://openapi.chzzk.naver.com/auth/v1/token";

                var response = await _httpClient.PostAsync(tokenEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[오시리스의 거절 상세 내역] HTTP {response.StatusCode} - {errorContent}");
                    return null; // 실패 시 null 반환
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<ChzzkTokenResponse>();

                if (tokenResponse != null && tokenResponse.Code == 200 && tokenResponse.Content != null && !string.IsNullOrEmpty(tokenResponse.Content.AccessToken))
                {
                    Console.WriteLine($"[텔로스5] Access Token 연성 성공! (만료: {tokenResponse.Content.ExpiresIn}초)");

                    // [수정됨]: true 대신 실제 토큰 문자열을 반환합니다.
                    return tokenResponse.Content.AccessToken;
                }
                else
                {
                    Console.WriteLine($"[오시리스의 거절] 응답은 성공했으나 토큰 알맹이가 없습니다.");
                    return null; // 실패 시 null 반환
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[하모니 경고] 치지직 인증 서버와의 공명에 실패: {ex.Message}");
                return null; // 실패 시 null 반환
            }
        }

        /// <summary>
        /// [파로스의 증명]: 획득한 토큰을 사용해 로그인한 스트리머의 정보를 가져옵니다.
        /// </summary>
        public async Task<ChzzkUserProfileContent?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                // 1. HTTP 요청 헤더에 Access Token을 장착합니다.
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // 2. 치지직 Open API '내 정보 조회' 호출
                var response = await _httpClient.GetAsync("https://openapi.chzzk.naver.com/open/v1/users/me");

                if (response.IsSuccessStatusCode)
                {
                    var profileResponse = await response.Content.ReadFromJsonAsync<ChzzkUserProfileResponse>();
                    return profileResponse?.Content;
                }

                Console.WriteLine($"[오시리스의 거절] 프로필 조회 실패: HTTP {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[하모니 경고] 프로필 파동 수신 오류: {ex.Message}");
                return null;
            }
        }
    }
}

