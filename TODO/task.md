# 📋 함대 재구축 작업 리스트 (EDMH Migration Task)

본 리스트는 **물멍(세피로스)** 지휘관님의 전략에 따라 작성된 아키텍처 수술용 Task 관리 대장입니다.

## 🏁 [0순위] 혈관 연결 : 공용 계약(Shared Contracts) 프로젝트 신설
- [x] `MooldangBot.Contracts` 클래스 라이브러리 프로젝트 생성
- [x] 핵심 인터페이스 이관 (`IChzzkApiClient`, `IMessagingSender` 등)
- [x] 공용 DTO 및 Event 모델 이관 (`ChatReceivedEvent`, `PointChangedEvent` 등)
- [x] `MooldangBot.Api`의 참조 수정 (Application -> Contracts)
- [x] `MooldangBot.ChzzkAPI`의 참조 수정 (Application -> Contracts)

## 🥇 [1순위] 가장 치명적인 위험 제거 : ChzzkAPI 통신 개편
- [ ] RabbitMQ 통신 패턴 변경 (RPC -> Fire & Forget Publish)
- [ ] `ChzzkCommandConsumer`의 응답 대기 로직 제거 및 비동기 처리 도입
- [ ] API 게이트웨이 측의 이벤트 핸들러(Subscriber) 구현
- [ ] 10k TPS 부하 테스트 및 스레드 블로킹 여부 검증

## 🥈 [2순위] 쉬운 도메인부터 분리 (Vertical Slicing)
- [ ] `MooldangBot.Modules.SongBook` 모듈 프로젝트 생성
- [ ] SongBook 도메인 로직 및 서비스 이관
- [ ] `MooldangBot.Modules.Roulette` 모듈 프로젝트 생성
- [ ] Roulette 도메인 로직 및 서비스 이관
- [ ] 독립 모듈 단위 빌드 및 배포 테스트

## 🥉 [3순위] 코어 도메인 분리 및 논리적 DB 스키마 분할
- [ ] `Chat` 도메인 독립 모듈화 및 로직 분리
- [ ] `Points` 도메인 독립 모듈화 및 로직 분리
- [ ] `ChatDbContext` 및 `PointDbContext` 생성 (AppDbContext로부터 분할)
- [ ] 도메인별 자치권 부여 및 서비스 간 이벤트 기반 데이터 동기화 검증

---
**상태**: 🚀 수술 대기 중 | **전략**: 스트랭글러 피그 패턴 (Strangler Fig)
