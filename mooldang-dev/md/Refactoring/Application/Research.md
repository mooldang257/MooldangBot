# MooldangBot.Application 모듈 상세 분석 보고서

## 1. 개요
`MooldangBot.Application`은 시스템의 비즈니스 로직과 워크플로우를 담당하는 핵심 계층입니다. 도메인 엔티티를 활용하여 실제 기능을 수행하며, 백그라운드 서비스와 이벤트 핸들러를 통해 치지직 플랫폼과 실시간으로 상호작용합니다.

---

## 2. 백그라운드 워커 (Workers)

### 2-1. ChzzkBackgroundService
- **역할**: 전체 봇 엔진의 컨트롤 타워입니다.
- **동작**: 60초마다 DB를 조회하여 `IsBotEnabled`가 활성화된 스트리머 채널에 대해 `ChzzkChannelWorker`를 생성하거나 관리합니다.

### 2-2. ChzzkChannelWorker (핵심 실시간 엔진)
- **역할**: 개별 스트리머 채널과 치지직 WebSocket 서버 간의 물리적 연결을 담당합니다.
- **핵심 로직**:
    - **WebSocket 연결**: Socket.io 프로토콜에 맞게 URL을 조립하고 핑/퐁(Ping/Pong) 루프를 통해 연결을 유지합니다.
    - **이벤트 디스패치**: 수신된 CHAT/DONATION 이벤트를 파싱하여 MediatR `ChatMessageReceivedEvent`를 발행합니다.
    - **토큰 자동 갱신**: 연결 직전 토큰 만료 여부를 확인하고 필요 시 자동으로 갱신(Refresh)합니다.

---

## 3. 기능별 핸들러 (Features - MediatR)

시스템은 이벤트 드리븐 아키텍처(EDA)를 채택하여, 채팅 한 건이 발생하면 다수의 핸들러가 병렬로 동작합니다.

- **CustomCommandEventHandler**: `!명령어` 형태의 텍스트를 감지하여 DB에 등록된 커스텀 응답을 반환하거나 특정 액션을 수행합니다.
- **RouletteEventHandler**: 룰렛 명령어 감지 시 확률 기반 추첨을 수행하고 결과를 SignalR을 통해 오버레이로 전달합니다.
- **ChatBroadcastEventHandler**: 수신된 채팅 메시지와 이모티콘 정보를 SignalR `OverlayHub`를 통해 실시간 오버레이 화면으로 전송합니다.
- **PointTransactionService**: 채팅 점수 적립, 후원 포인트 가산, 룰렛 비용 차감 등 시청자 경제 시스템의 모든 트랜잭션을 처리합니다.

---

## 4. 핵심 서비스 (Services)

### 4-1. ChzzkBotService (봇 계정 관리자)
- **토큰 우선순위 전략**:
    1. 스트리머가 설정한 **커스텀 봇 계정**.
    2. 시스템 전역 설정에 등록된 **공통 봇 계정**.
    3. 위 계정들이 없을 경우 **스트리머 본인 계정**으로 폴백.
- **역할**: 적절한 토큰을 사용하여 치지직 오픈 API를 통해 채팅 답장을 발송합니다.

### 4-2. SongBookService
- 스트리머의 곡 목록(레퍼토리) 관리 및 곡 추가/삭제 로직을 담당합니다.

---

## 5. 상태 관리 (State)
- **SongQueueState / RouletteState**: 인메모리 싱글톤 객체로 현재 신청곡 리스트나 활성화된 룰렛 상태를 관리하여 API와 워커 간의 데이터 정합성을 유지합니다.

---
*분석 완료 - 2026-03-27 물멍(AI)*
