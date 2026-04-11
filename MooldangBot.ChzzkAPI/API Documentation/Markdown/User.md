# User

유저 API로 로그인 유저의 치지직 채널 정보를 조회할 수 있습니다.\
유저 API를 호출하려면 사용자 계정으로 인증하여 얻은 Access Token이 필요합니다.\
API Scope는 `유저 정보 조회`입니다.

## 유저 정보 조회

유저의 채널 정보를 조회할 수 있습니다.\
치지직의 모든 유저는 채널을 소유합니다. 채널ID는 채널의 고유 식별자이며 유저의 고유 식별자로 사용할 수 있습니다.

<table><thead><tr><th width="262">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/users/me</strong></td><td>유저 정보 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="182">Key</th><th width="174">Type</th><th>Example</th></tr></thead><tbody><tr><td>channelId</td><td>String</td><td>909501f048b44cf0d5c1d28XXXXXXXX</td></tr><tr><td>channelName</td><td>String</td><td>치지직유저 3121</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>