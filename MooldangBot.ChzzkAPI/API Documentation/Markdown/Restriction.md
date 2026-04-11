# Restriction

활동 제한 API로 활동 제한 추가, 삭제, 목록 조회를 할 수 있습니다.\
활동 제한을 호출하려면 사용자 계정으로 인증하여 얻은 Access Token이 필요합니다.\
API Scope는 `활동 제한 추가`, `활동 제한 삭제`, `활동 제한 목록 조회`,`임시제한 추가`, `임시제한 삭제`입니다.

***

## 활동 제한 추가

사용자를 활동 제한 대상으로 추가할 수 있습니다.

<table><thead><tr><th width="284">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>POST /open/v1/restrict-channels</strong></td><td>활동 제한 추가</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="159.33331298828125">Field</th><th width="126">Type</th><th>Description</th></tr></thead><tbody><tr><td>targetChannelId</td><td>String</td><td>활동 제한 대상 channelId</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="160">Code</th><th>Description</th></tr></thead><tbody><tr><td>200</td><td>활동 제한 추가 성공</td></tr><tr><td></td><td></td></tr></tbody></table>

## 활동 제한 삭제

사용자를 활동 제한 대상에서 삭제할 수 있습니다.

<table><thead><tr><th width="287">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>DELETE /open/v1/restrict-channels</strong></td><td>활동 제한 삭제</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="166.6666259765625">Field</th><th width="119">Type</th><th>Description</th></tr></thead><tbody><tr><td>targetChannelId</td><td>String</td><td>활동 제한 삭제 대상 channelId</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

**Response**

<table><thead><tr><th width="166.666748046875">Code</th><th>Description</th></tr></thead><tbody><tr><td>200</td><td>활동 제한 삭제 성공</td></tr><tr><td></td><td></td></tr></tbody></table>

## 활동 제한 목록 조회

채널의 활동   제한 목록을 조회할 수 있습니다.

<table><thead><tr><th width="285">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/restrict-channels</strong></td><td>활동 제한 목록 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="166.6666259765625">Field</th><th width="119">Type</th><th>Description</th></tr></thead><tbody><tr><td>size</td><td>Integer</td><td>조회할 목록의 크기 (기본값 : 30, 최대 30)</td></tr><tr><td>next</td><td>String</td><td>다음 페이징 값으로 사용</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="290">Field</th><th width="100">Type</th><th>Description</th></tr></thead><tbody><tr><td>restrictedChannelId</td><td>String</td><td>활동 제한 대상 channelId</td></tr><tr><td>restrictedChannelName</td><td>String</td><td>활동 제한 대상 채널명</td></tr><tr><td>createdDate</td><td>Date</td><td>활동 제한 일자</td></tr><tr><td>releaseDate</td><td>Date</td><td>활동 제한 해제 일자</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

## 임시제한 추가

사용자를 임시제한 대상으로 추가할 수 있습니다.

<table><thead><tr><th width="284">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>POST /open/v1/temporary-restrict-channels</strong></td><td>임시제한 추가</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="159.33331298828125">Field</th><th width="126">Type</th><th>Description</th></tr></thead><tbody><tr><td>targetChannelId</td><td>String</td><td>임시제한 대상 channelId</td></tr><tr><td>chatChannelId</td><td>String</td><td>채팅 channelId</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="160">Code</th><th>Description</th></tr></thead><tbody><tr><td>200</td><td>임시제한 추가 성공</td></tr><tr><td>400</td><td>존재하지 않는 사용자 / 임시제한된 사용자 / 등록이 불가능한 계정</td></tr><tr><td>403</td><td>권한 없음</td></tr><tr><td></td><td></td></tr></tbody></table>

## 임시제한 해제

사용자를 활동 제한 대상에서 삭제할 수 있습니다.

<table><thead><tr><th width="287">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>DELETE /open/v1/temporary-restrict-channels</strong></td><td>임시제한 해제</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="166.6666259765625">Field</th><th width="119">Type</th><th>Description</th></tr></thead><tbody><tr><td>targetChannelId</td><td>String</td><td>임시제한 해제 대상 channelId</td></tr><tr><td>chatChannelId</td><td>String</td><td>채팅 channelId</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>

**Response**

<table><thead><tr><th width="166.666748046875">Code</th><th>Description</th></tr></thead><tbody><tr><td>200</td><td>임시제한 해제 성공</td></tr><tr><td>400</td><td>존재하지 않는 사용자 / 해제가 불가능한 계정</td></tr><tr><td>403</td><td>권한 없음</td></tr><tr><td></td><td></td></tr></tbody></table>