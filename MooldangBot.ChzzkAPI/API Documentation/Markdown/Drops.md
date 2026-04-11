# Drops

치지직에서 인앱보상 유형의 드롭스 이벤트 진행 시 지급 여부 검증을 위한 가이드 문서입니다.

드롭스의 기본적인 설명 및 연동 가이드는 다음 문서를 참고해 주세요.

* [드롭스 개요](https://chzzk.gitbook.io/chzzk/drops/overview)
* [드롭스 연동 및 절차](https://chzzk.gitbook.io/chzzk/drops/guide)

## 1.1 리워드 지급 요청 이벤트 받기

치지직에서 사용자가 드롭스 리워드를 획득하는 과정에서, 게임사에 지급 요청을 할 수 있습니다. 이때 치지직에서는 등록된 게임사의 webhook URL로 notification 메시지를 전달합니다.

\*Note : 상세 내용은 ‘치지직 Webhook Event 가이드’ 문서를 참고해주세요.

notification 메시지는 별도 callback을 요구하지 않습니다. 따라서 드롭스 리워드 지급 상태를 변경하기 위해서는 드롭스 리워드 지급 API를 호출해야 합니다.<br>

## 1.2 드롭스 리워드 지급 요청 조회 API

치지직 사용자의 드롭스 리워드 지급 요청을 조회합니다.  드롭스 API를 호출하기 위해서는 드롭스 API Scope 신청이 필요합니다. 해당 API Scope는 개발자 센터에 로그인된 사용자가 단체 ID로 법인 인증된 사용자여야만 신청할 수 있습니다.

드롭스 리워드 지급 요청 API를 호출하려면 애플리케이션 등록 후 Client 인증이 필요합니다. ([Client 인증 API 참조](https://chzzk.gitbook.io/chzzk/chzzk-api/tips#client-api))

<table><thead><tr><th width="395">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/drops/reward-claims</strong></td><td>드롭스 리워드 지급 요청 조회</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Param**

<table><thead><tr><th width="221">Field</th><th width="174">Type</th><th width="109">Required</th><th>Description</th></tr></thead><tbody><tr><td>page</td><td>Object</td><td>Optional</td><td>페이징 조회를 위해 사용</td></tr><tr><td>    from</td><td>String</td><td>Optional</td><td>페이징 조회 시, 조회 첫 기준 식별자</td></tr><tr><td>    size</td><td>Int</td><td>Optional</td><td>페이징 조회 시, 조회 할 전체 크기. 기본 20</td></tr><tr><td>claimId</td><td>String</td><td>Optional</td><td>조회할 지급 요청 ID. 콤마(,)로 구분된 배열로 최대 100개까지 요청 가능합니다.</td></tr><tr><td>channelId</td><td>String</td><td>Optional</td><td>조회할 유저의 채널 ID</td></tr><tr><td>campaignId<br>or<br>categoryId</td><td>String</td><td>Optional</td><td><p>특정 캠페인 및 카테고리로 조회할 때 조건으로 사용합니다. 동시에 두가지 조건을 사용할 수 없습니다.</p><ul><li>campaignId: 조회할 드롭스 캠페인 ID</li><li>category: 치지직 카테고리 기준, 조회할 방송(게임) 카테고리 ID</li></ul></td></tr><tr><td>fulfillmentState</td><td>String</td><td>Optional</td><td><p>조회할 리워드 상태 조건. 명시되지 않으면 모든 상태에 대해 조회합니다.</p><ul><li><code>CLAIMED</code>: 지급 요청된 리워드</li><li><code>FULFILLED</code>: 지급 완료</li></ul></td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="218">Field</th><th width="177">Type</th><th width="110">Required</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>*</td><td>응답 정보가 담긴 객체 배열. 조회 결과가 없으면 빈 배열이 응답될 수 있습니다.</td></tr><tr><td>    claimId</td><td>String</td><td>*</td><td>드롭스 캠페인 지급 요청 ID</td></tr><tr><td>    campaignId</td><td>String</td><td>*</td><td>드롭스 캠페인 ID</td></tr><tr><td>    rewardId</td><td>String</td><td>*</td><td>드롭스 캠페인에 포함된 리워드 ID</td></tr><tr><td>    categoryId</td><td>String</td><td>*</td><td>드롭스 캠페인에 할당된 카테고리 ID</td></tr><tr><td>    categoryName</td><td>String</td><td>*</td><td>드롭스 캠페인에 할당된 노출 가능한 카테고리 이름</td></tr><tr><td>    channelId</td><td>String</td><td>*</td><td>사용자의 치지직 채널 ID</td></tr><tr><td>    fulfillmentState</td><td>String</td><td>*</td><td><p>조회할 리워드 상태 조건</p><p>  CLAIMED: 지급 요청된 리워드</p><p>  FULFILLED: 지급 완료</p></td></tr><tr><td>    claimedDate</td><td>String</td><td>*</td><td>마지막으로 지급 요청된 시간. RFC3339 형식 UTC 시간</td></tr><tr><td>    updatedDate</td><td>String</td><td>*</td><td>마지막으로 fulfillmentState 값이 변경된 시간. RFC3339 형식 UTC 시간</td></tr><tr><td>page</td><td>Object</td><td>*</td><td>페이징 조회를 위해 사용.</td></tr><tr><td>    cursor</td><td>String</td><td>Optional</td><td>페이징 조회 시, 다음 조회 시 page.from으로 사용. Request Param에서 claimId 항목이 정의된 경우 값이 없거나 빈 문자열일 수 있습니다.</td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

```json
{
    "data": [
        {
            "claimId": "1ce167db-8c58-4a37-8662-48d4e2398503",
            "campaignId": "bb3a5e21-b18a-4b31-851b-adbecd7dfae5",
            "rewardId": "068598a1-36b2-4bb4-ab8b-dd875e621862",
            "categoryId": "CATEGORY_CHZZK",
            "categoryName": "치지직",
            "channelId": "34d7ff19-1b63-4b32-85fb-d44880195533",
            "fulfillmentState": "CLAIMED",
            "claimedDate": "2024-08-01T09:20:26Z",
            "updatedDate": "2024-08-01T09:20:26Z"
        },
        {
            "claimId": "6dd13087-d811-479d-8c4d-52d9f3a54c24",
            "campaignId": "bb3a5e21-b18a-4b31-851b-adbecd7dfae5",
            "rewardId": "13708b6d-e0ba-4180-982b-f16aa3521d15",
            "categoryId": "CATEGORY_CHZZK",
            "categoryName": "치지직",
            "channelId": "34d7ff19-1b63-4b32-85fb-d44880195533",
            "fulfillmentState": "FULFILLED",
            "claimedDate": "2024-08-01T09:21:26Z",
            "updatedDate": "2024-08-01T09:22:26Z"
        },
        {
            "claimId": "477fe295-30c2-4670-a3a6-6ad8305d36ad",
            "campaignId": "bb3a5e21-b18a-4b31-851b-adbecd7dfae5",
            "rewardId": "13708b6d-e0ba-4180-982b-f16aa3521d15",
            "categoryId": "CATEGORY_CHZZK",
            "categoryName": "치지직",
            "channelId": "991cd681-527d-43e5-9758-adae7905a005",
            "fulfillmentState": "FULFILLED",
            "claimedDate": "2024-08-01T09:22:26Z",
            "updatedDate": "2024-08-01T09:23:26Z"
        }
    ],
    "page": {
        "cursor": "2af178e3-a620-43da-b4b6-90fe11fa0737"
    }
}

```

## 1.3 드롭스 리워드 지급 API

치지직 사용자의 드롭스 리워드의 지급 상태를 관리합니다. 드롭스 API를 호출하기 위해서는 드롭스 API Scope 신청이 필요합니다. 해당 API Scope는 개발자 센터에 로그인된 사용자가 단체 ID로 법인 인증된 사용자여야만 신청할 수 있습니다.

드롭스 리워드 지급 API는 별도의 헤더가 존재합니다. \
드롭스 리워드 지급 API를 호출하려면 애플리케이션 등록 후 Client 인증이 필요합니다. ([Client 인증 API 참조](https://chzzk.gitbook.io/chzzk/chzzk-api/tips#client-api))

<table><thead><tr><th width="340">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>PUT /open/v1/drops/reward-claims</strong></td><td>드롭스 리워드 지급</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Body**

<table><thead><tr><th width="195">Field</th><th width="144">Type</th><th width="106">Required</th><th>Description</th></tr></thead><tbody><tr><td>claimIds</td><td>String[]</td><td>*</td><td>드롭스 캠페인 지급 요청 ID 배열</td></tr><tr><td>fulfillmentState</td><td>String</td><td>*</td><td><p>조회할 리워드 상태 조건</p><ul><li><code>CLAIMED</code>: 지급 요청된 리워드</li><li><code>FULFILLED</code>: 지급 완료</li></ul></td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

```json
{
    "claimIds": ["6dd13087-d811-479d-8c4d-52d9f3a54c24"]
    "fulfillmentState": "FULFILLED"
}
```

**Response Body**

<table><thead><tr><th width="195">Field</th><th width="141">Type</th><th width="109">Required</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>*</td><td>결과가 담긴 배열. data[].status의 따라 구분</td></tr><tr><td>    status</td><td>String</td><td>*</td><td><p>지급 상태를 명시</p><ul><li><code>INVALID_ID</code>: 잘못된 지급 요청 ID 입니다.</li><li><code>NOT_FOUND</code>: 해당하는 지급 ID를 찾을 수 없습니다.</li><li><code>SUCCESS</code>: 성공적으로 변경했습니다.</li><li><code>UNAUTHORIZ</code>ED: 요청한 유저가 계정 연동을 해제한 경우입니다.</li><li><code>UPDATE_FAILED</code>: 에러가 발생하여 상태 변경이 실패했습니다.</li></ul></td></tr><tr><td>    ids</td><td>String[]</td><td>*</td><td>지급 상태에 해당하는 지급 요청 ID 배열</td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

```json
{
    "data": [
        {
            "status": "SUCCESS",
            "ids": ["1ce167db-8c58-4a37-8662-48d4e2398503", "6dd13087-d811-479d-8c4d-52d9f3a54c24", "477fe295-30c2-4670-a3a6-6ad8305d36ad"]
        },
        {
            "status": "UNAUTHORIZED",
            "ids": ["3ff167ah-912g-9112-1001-1k2fmvakppzp", "9avlake5-9z1s-8710-b0ba-1ga99b1123fd", "ab98c1z5-mz21-1219-nex0-akem19am7bb"]
        }
    ]
}

```