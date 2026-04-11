# ChzzkAPI 마이크로서비스 리팩토링 복구 상황판 (v3.3)

`ChzzkAPI`를 완전한 Standalone Gateway(DB-less)로 전환하기 위한 모델 재건 및 최종 연동 작업입니다.

- [x] **Phase 1: 인프라 구축 및 기초 수립**
    - [x] `Contracts` 프로젝트 생성 및 참조 정리
    - [x] `ChzzkJsonContext.cs` 복구
    - [x] `ChzzkApiResponse`, `ChzzkPagedResponse` (Shared) 모델 복구

- [x] **Phase 2: 도메인 기반 모델 재건 (Contracts)**
    - [x] **Batch 1: 기초 도메인** (Auth: TokenRequest, TokenResponse 등)
    - [x] **Batch 2: 메타데이터 도메인** (User, Channel, Category)
    - [x] **Batch 3: 실시간 서비스 도메인** (Live, Session, Chat)
    - [x] **Batch 4: 정책 및 보상 도메인** (Drops, Restriction)

- [ ] **Phase 5: 게이트웨이 독립 기능 강화 (ChzzkAPI)**
    - [/] `InMemoryChzzkTokenStore` 및 `RabbitMqChzzkMessagePublisher` 최종 검증
    - [x] `InternalTokenController` 구현 (보안 키 적용)
    - [x] `Clients/ChzzkApiClient` 재건 완료
    - [x] `Sharding/WebSocketShard` 재건 완료
    - [/] `Sharding/ShardedWebSocketManager` 복구 진행 중
    - [ ] `Workers/ChzzkCommandConsumer` 검증 및 복구

- [ ] **Phase 6: 최종 빌드 및 검증**
    - [ ] 개별 프로젝트 빌드 성공
    - [ ] 봇 엔진(Application) 연동 테스트
