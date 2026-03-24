# 🪙 ChatPoint & Attendance 도메인 상세 가이드

> **[🤖 AI 파트너를 위한 지시어 (System Prompt)]**
> 이 문서는 채팅포인트, 출석, 룰렛 소비와 관련된 도메인 지식입니다. 해당 도메인의 CRUD 로직이나 이벤트를 수정할 때 이 문서의 **상태 변화 테이블**과 **BDD 시나리오**를 반드시 준수하여 코드를 작성하십시오.

---

## 1. 🔄 상태 변화 및 트랜잭션 매핑 테이블 (State Mutation)
*로직 수정 시 반드시 아래의 트랜잭션 범위와 생명주기를 준수하십시오.*

| 트리거 (Trigger) | 타겟 핸들러 | 상태 읽기 (Read DB / API) | 상태 쓰기 (Write DB) | 외부 발행 (Emit Event / SignalR) |
| :--- | :--- | :--- | :--- | :--- |
| **일반 채팅 수신** | `ViewerPointEventHandler` | `ViewerProfile` (조회/생성)<br>`StreamerProfile` | `.Points += PointPerChat` | **없음** |
| **출석 명령어 수신** | `ViewerPointEventHandler` | `ViewerProfile` (조회/생성)<br>`StreamerProfile` | `.Points += 포인트 + 출석보너스`<br>`.AttendanceCount++`<br>`.LastAttendanceAt` = KST Now | **봇 채팅 전송:** `AttendanceReply` |
| **치즈 후원 수신** | `ViewerPointEventHandler` | `ViewerProfile` (조회/생성)<br>`StreamerProfile` | `.Points += 후원보너스`<br>*(일반 포인트와 합산)* | **없음** |
| **포인트 조회 명령어** | `CustomCommandEventHandler` | `ViewerProfile`<br>**[API]** 치지직 팔로우 일수 | **없음** | **봇 채팅 전송:** `PointCheckReply` |
| **포인트 룰렛 성공** | `RouletteEventHandler` | `ViewerProfile` | `.Points -= CostPerSpin` | **SignalR:** `RouletteTriggered` |

---

## 2. 📊 핵심 데이터 모델 및 계산 공식

### 2-1. 주요 엔터티
* **`ViewerProfile`:** 시청자별 데이터. `(StreamerChzzkUid, ViewerUid)` 복합 유니크 인덱스로 식별. (`Points`, `AttendanceCount`, `LastAttendanceAt` 등 보유)
* **`StreamerProfile`:** 포인트 정책 설정. (`PointPerChat`, `PointPerDonation1000`, `PointPerAttendance`, `AttendanceCommands` 등 보유)

### 2-2. 포인트 최종 합산 공식
이벤트 핸들러 내에서 DB `Save`는 비용 최적화를 위해 **최종 1회만 수행**합니다.
```text
최종 추가 포인트 (pointToAdd) = 기본 채팅 포인트
+ (오늘 KST 첫 출석인 경우 ? 출석 보너스 : 0)
+ (치즈 후원이 있는 경우 ? (후원금/1000) * 후원 가중치 : 0)