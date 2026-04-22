# Channel

채널 API로 채널 정보 조회, 채널 관리자 조회, 채널 팔로워 조회, 채널 구독자를 조회할 수 있습니다.\
채널 API 중 아래 API Scope를 호출하려면 사용자 계정으로 인증하여 얻은 Access Token이 필요합니다.\
API Scope는 `채널 관리자 조회`, `채널 팔로워 조회`, `채널 구독자 조회`입니다.

***

## 채널 정보 조회

채널 정보를 조회할 수 있습니다.\
채널 정보 조회 API를 호출하려면 애플리케이션 등록 후 Client 인증이 필요합니다. ([Client 인증 API 참조](https://chzzk.gitbook.io/chzzk/chzzk-api/tips#client-api))

<table><thead><tr><th width="262">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/channels</strong></td><td>채널 정보 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Param**

<table><thead><tr><th width="134">Field</th><th width="126">Type</th><th width="94">required</th><th>Description</th></tr></thead><tbody><tr><td>channelIds</td><td>String[]</td><td>*</td><td>조회할 채널 ID 목록. 최대 20개까지 요청 가능</td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="261">Field</th><th width="98">Type</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>요청한 채널 정보 목록. 일치하는 채널을 찾지 못할 경우 결과 미반환</td></tr><tr><td>    channelId</td><td>String</td><td>채널 식별자</td></tr><tr><td>    channelName</td><td>String</td><td>채널 이름</td></tr><tr><td>    channelImageUrl</td><td>String</td><td>채널 이미지 URL</td></tr><tr><td>    followerCount</td><td>Int</td><td>채널의 팔로워 수</td></tr><tr><td>    verifiedMark</td><td>Boolean</td><td>채널 인증 마크 여부</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

## 채널 관리자 조회

채널 관리자를 조회할 수 있습니다.

<table><thead><tr><th width="355">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/channels/streaming-roles</strong></td><td>채널 관리자 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="261">Field</th><th width="98">Type</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>요청한 채널 관리자 목록</td></tr><tr><td>    managerChannelId</td><td>String</td><td>관리자 채널 식별자</td></tr><tr><td>    managerChannelName</td><td>String</td><td>관리자 채널 이름</td></tr><tr><td>    userRole</td><td>String</td><td><p>관리자 역할</p><ul><li>STREAMING_CHANNEL_OWNER - 채널 소유자</li><li>STREAMING_CHANNEL_MANAGER - 채널 관리자</li><li>STREAMING_CHAT_MANAGER - 채팅 운영자</li><li>STREAMING_SETTLEMENT_MANAGER - 정산 관리자</li></ul></td></tr><tr><td>    createdDate</td><td>Date</td><td>등록일</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

## 채널 팔로워 조회

채널의 팔로워 목록을 조회할 수 있습니다.

<table><thead><tr><th width="355">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/channels/followers</strong></td><td>채널 팔로워 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Param**

<table><thead><tr><th width="134">Field</th><th width="126">Type</th><th width="94">required</th><th>Description</th></tr></thead><tbody><tr><td>page</td><td>Int</td><td>optional</td><td><p>요청하는 페이지. 0부터 시작</p><p>디폴트 값 : 0</p></td></tr><tr><td>size</td><td>Int</td><td>optional</td><td>조회할 카테고리 개수. 최소 1 ~ 최대 50 요청 가능<br>디폴트 값 : 30</td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="261">Field</th><th width="98">Type</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>요청한 채널의 팔로워 목록</td></tr><tr><td>    channelId</td><td>String</td><td>팔로워 채널 식별자</td></tr><tr><td>    channelName</td><td>String</td><td>팔로워 채널 이름</td></tr><tr><td>    createdDate</td><td>Date</td><td>팔로우 일자</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

## 채널 구독자 조회

채널의 구독자 목록을 조회할 수 있습니다.

<table><thead><tr><th width="355">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/channels/subscribers</strong></td><td>채널 구독자 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Param**

<table><thead><tr><th width="134">Field</th><th width="126">Type</th><th width="94">required</th><th>Description</th></tr></thead><tbody><tr><td>page</td><td>Int</td><td>optional</td><td><p>요청하는 페이지. 0부터 시작</p><p>디폴트 값 : 0</p></td></tr><tr><td>size</td><td>Int</td><td>optional</td><td>조회할 카테고리 개수. 최소 1 ~ 최대 50 요청 가능<br>디폴트 값 : 30</td></tr><tr><td>sort</td><td>String</td><td>optional</td><td><p>정렬 방식</p><ul><li>RECENT (최신 구독 순)</li><li>LONGER (구독 개월 순)</li></ul></td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="261">Field</th><th width="98">Type</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>요청한 채널의 구독자 목록</td></tr><tr><td>    channelId</td><td>String</td><td>구독자 채널 식별자</td></tr><tr><td>    channelName</td><td>String</td><td>구독자 채널 이름</td></tr><tr><td>    month</td><td>Int</td><td>구독 개월 수</td></tr><tr><td>    tierNo</td><td>Int</td><td><p>구독 상품</p><ul><li>1 (티어1 구독)</li><li>2 (티어2 구독)</li></ul></td></tr><tr><td>    createdDate</td><td>Date</td><td>팔로우 일자</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>