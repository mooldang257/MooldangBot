# Category

카테고리 검색 API로 카테고리 목록 및 정보를 조회할 수 있습니다.

{% hint style="info" %}
방송은 개별 게임 카테고리 또는 종합 게임, 데모 게임, 고전 게임, 스포츠, 축구, 야구, talk, ASMR, 음악/노래, 그림/아트, 운동/건강, 과학/기술, 시사/경제, 먹방/쿡방, 뷰티, 여행/캠페인 카테고리로 분류될 수 있습니다.
{% endhint %}

## 카테고리 검색

카테고리를 검색하여 목록 및 정보를 조회할 수 있습니다.\
카테고리 검색 API를 호출하려면 애플리케이션 등록 후 Client 인증이 필요합니다. ([Client 인증 API 참조](https://chzzk.gitbook.io/chzzk/chzzk-api/tips#client-api))

<table><thead><tr><th width="355">HTTP Request</th><th>Description</th></tr></thead><tbody><tr><td><strong>GET /open/v1/categories/search</strong></td><td>카테고리 검색</td></tr><tr><td></td><td></td></tr></tbody></table>

**Request Param**

<table><thead><tr><th width="134">Field</th><th width="126">Type</th><th width="94">required</th><th>Description</th></tr></thead><tbody><tr><td>size</td><td>Int</td><td>optional</td><td>조회할 카테고리 개수. 최소 1 ~ 최대 50 요청 가능<br>디폴트 값 : 20</td></tr><tr><td>query</td><td>String</td><td>*</td><td>검색할 카테고리 이름. 해당 값을 포함하는 카테고리 목록 반환</td></tr><tr><td></td><td></td><td></td><td></td></tr></tbody></table>

**Response Body**

<table><thead><tr><th width="182">Field</th><th width="174">Type</th><th>Description</th></tr></thead><tbody><tr><td>data</td><td>Object[]</td><td>카테고리 목록 결과</td></tr><tr><td>categoryType</td><td>String</td><td><p>카테고리 종류</p><ul><li>GAME</li><li>SPORTS</li><li>ETC</li></ul></td></tr><tr><td>categoryId</td><td>String</td><td>카테고리 ID(카테고리 식별자)</td></tr><tr><td>categoryValue</td><td>String</td><td>카테고리 이름</td></tr><tr><td>posterImageUrl</td><td>String</td><td>카테고리 이미지 URL</td></tr><tr><td></td><td></td><td></td></tr></tbody></table>