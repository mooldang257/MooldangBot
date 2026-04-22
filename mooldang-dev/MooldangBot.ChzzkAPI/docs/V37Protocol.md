# v3.7 통신 규약 명세 (Communication Protocol Specification)

본 문서는 `MooldangBot v3.7`에서 확정된 치지직 게이트웨이와 메인 앱 간의 통신 규약을 정의합니다. 향후 파이프라인 수정 시 본 규약을 반드시 준수해야 합니다.

---

## 1. 메시지 브로커 설정 (RabbitMQ)

### 1.1 통합 익스체인지 (Unified Exchange)
- **이름**: `mooldang.chzzk.chat`
- **타입**: `topic`
- **목적**: 치지직에서 발생하는 모든 소켓 이벤트를 메인 앱으로 전달하는 단일 통로.

### 1.2 라우팅 키 (Routing Key)
- `chat`: 일반 채팅 메시지
- `donation`: 채팅 후원 및 영상 후원
- `subscription`: 치지직 구독(치즈 수수 등) 이벤트

---

## 2. 공통 이벤트 엔벨로프 (Event Envelope)

모든 이벤트는 `ChzzkEventEnvelope` 클래스에 감싸져 전달됩니다.

```json
{
  "EventType": "Chat | ChatDonation | VideoDonation | Subscription",
  "ChannelId": "c74931e68d4d90ce9f11d6f343c1d54c",
  "SentAt": "2026-04-12T07:51:54.016Z",
  "Data": { ... 구체적인 이벤트 페이로드 ... }
}
```

---

## 3. 데이터 추출 및 매핑 규칙 (v3.7 핵심)

이벤트 데이터를 추출할 때는 **공식 문서(`Session.md`)**와 **실전 데이터**의 차이를 고려하여 다음 우선순위를 따릅니다.

### 3.1 일반 채팅 (Chat)
- **SenderId**: `senderChannelId` (루트) 우선 $\rightarrow$ 없을 경우 `channelId` (루트) 사용.
- **UserRole**: `userRoleCode` (루트) $\rightarrow$ `profile.userRoleCode` $\rightarrow$ `extras.userRoleCode` 순서로 확인.
- **Content**: `content` 필드 사용.

### 3.2 후원 (Donation)
- **DonationType 판별**: 
  1. 원본 페이로드의 `donationType` (루트) 확인.
  2. `VIDEO`면 `VideoDonation`, `CHAT`이면 `ChatDonation`으로 분류.
- **금액(Amount)**: `payAmount` (루트) $\rightarrow$ `extras.payAmount` 순서로 확인. (String/Number 타입 모두 대응)
- **메시지**: `donationText` (루트) 사용.

### 3.3 구독 (Subscription)
- **Tier**: `tierNo`, `tierName` 루트 필드 사용.
- **기간**: `month` 루트 필드 사용.

---

## 4. 로깅 및 추적 (Traceability)

- **MessageId**: 각 이벤트에는 고유한 `MessageId`가 부여되어 게이트웨이와 메인 앱 로그에서 동일한 이벤트를 추적할 수 있어야 합니다.
- **Raw Diagnostic**: 문제 발생 시 진단을 위해 게이트웨이의 `Debug` 레벨 로그에서 원본 JSON(`Raw Payload`)을 확인할 수 있도록 유지합니다.

---

## 5. 수정 시 주의사항
- **Backward Compatibility**: `extras` 필드는 공식 문서에서 점차 사라지고 있으나, 시뮬레이터나 레거시 호환성을 위해 **Fallback**으로 반드시 유지해야 합니다.
- **Serialization**: `System.Text.Json`의 `JsonPropertyName` 어노테이션을 사용하여 필드 대소문자 불일치 문제를 방지합니다.
