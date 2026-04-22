# MooldangBot.Api 모듈 상세 분석 보고서

## 1. 개요
`MooldangBot.Api`는 전체 애플리케이션의 엔트리 포인트(Entry Point)이자 호스트입니다. 서비스 부트스트래핑, 환경 설정 매핑, 정적 파일 서빙, 그리고 런타임 데이터베이스 관리를 담당합니다.

---

## 2. 부트스트래핑 및 환경 설정 (Program.cs)

### 2-1. Zero-Git 환경 변수 전략
- **동적 로드**: 실행 파라미터(`--env=...`)를 통해 `.env` 파일 경로를 유동적으로 선택할 수 있습니다.
- **Smart Mapping**: 단일 `.env` 파일 내에서 `DEV_`, `PROD_` 접두사가 붙은 변수를 실행 환경(`ASPNETCORE_ENVIRONMENT`)에 맞춰 실제 변수로 자동 매핑합니다. 이는 다중 환경 배포 시 설정 실수를 최소화합니다.

### 2-2. 리버스 프록시 대응
- **ForwardedHeaders**: Cloudflare Tunnel이나 Nginx 뒤에서 동작할 때 클라이언트의 실제 IP와 프로토콜(HTTPS)을 정확히 인식할 수 있도록 `X-Forwarded-For`, `X-Forwarded-Proto` 헤더를 신뢰하도록 설정되어 있습니다.

### 2-3. 런타임 DB 관리 (Schema Maintenance)
- 애플리케이션 시작 시 DB 컨텍스트를 사용하여 다음 작업을 수행합니다:
    - `SystemSettings` 테이블에 초기 API 키 및 봇 토큰 동기화.
    - `songbooks`, `roulettelogs` 등 필요한 테이블이 없을 경우 `CREATE TABLE IF NOT EXISTS`를 통해 자동 생성.
    - 기존 테이블에 필요한 컬럼(`IsMission` 등)이 누락된 경우 `ALTER TABLE`을 통해 스키마 업데이트.

---

## 3. 프론트엔드 아키텍처 (wwwroot)

### 3-1. Vanilla JS 기반 고기능 페이지
- 별도의 프레임워크(React/Vue) 없이 순수 자바스크립트와 CSS를 사용하여 고도의 인터랙티브 페이지를 구현했습니다.
- **overlay_manager.html**: 2000라인이 넘는 규모의 드래그 앤 드롭, 리사이징, 실시간 미리보기가 가능한 오버레이 편집 도구입니다.

### 3-2. 실시간 연동
- 모든 관리 페이지와 오버레이는 SignalR(`microsoft-signalr`) 클라이언트를 포함하고 있으며, 백엔드의 `OverlayHub`와 실시간으로 상태를 동기화합니다.
- `localStorage`를 활용하여 사용자 설정(UID 등)을 브라우저에 유지합니다.

---

## 4. 라우팅 전략
- **MapControllers**: `Presentation` 계층의 컨트롤러들을 API 엔드포인트로 노출합니다.
- **MapHub**: `/overlayHub` 경로를 통해 실시간 웹소켓 통신을 제공합니다.
- **MapGet("/")**: 루트 접속 시 자동으로 대시보드(`/bot`)로 리다이렉트합니다.

---
*분석 완료 - 2026-03-27 물멍(AI)*
