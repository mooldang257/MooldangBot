# 프로젝트 인프라 구조 개편 및 AI 검색 엔진 배포 작업 보고서 (2026-04-22)

## 1. 개요
기존의 미러링 기반 아키텍처를 폐기하고, 물리적으로 분리된 `mooldang-dev/`와 `mooldang-prod/` 폴더 구조로 개편하였습니다. 또한 AI 기반의 송북 검색 엔진 고도화 사항을 메인 저장소에 반영하였습니다.

## 2. 주요 작업 내용

### A. 소스 코드 및 Git 관리
- **Git Push**: AI 기반 송북 검색 엔진(초성 검색, 오타 교정 등) 및 DB 벡터 마이그레이션 사항을 `main` 브랜치에 푸시 완료.
- **구조 개편**: 모든 소스 코드를 `mooldang-dev/` 폴더로 이동하여 개발 환경의 중심지로 설정.

### B. 개발 환경 (Dev) 구축 (`mooldang-dev/`)
- **컨테이너 분리**: 모든 서비스에 `mooldang-dev-` 접두어를 부여하여 운영 환경과 이름 충돌 방지.
- **포트 격리**: 운영 환경과 겹치지 않도록 개발 전용 포트 할당 (예: DB 3307, Redis 6380, Traefik 81).
- **도메인 설정**: `dev.mooldang.store`를 통해 접속하도록 설정.
- **환경 설정**: `.env` 파일을 개발 환경용(Development mode, dev path)으로 최적화.

### C. 운영 환경 (Prod) 구축 (`mooldang-prod/`)
- **이미지 기반 실행**: 소스 코드 없이 도커 이미지(`.tar`)와 설정 파일만으로 실행되도록 `docker-compose.yml` 수정.
- **설정 독립**: `traefik`, `grafana`, `prometheus` 등 주요 설정 파일을 운영 폴더 내에 복사하여 자급자족 구조 완성.
- **도메인 설정**: 기존 운영 도메인(`bot.mooldang.store`, `www.mooldang.store`) 유지.

### D. 이미지 관리 체계 (`mooldang-images/`)
- **전용 폴더 생성**: 빌드된 도커 이미지를 파일 형태로 명시적으로 관리하기 위한 `mooldang-images/` 생성.
- **자동 추출 자동화**: `build.sh` 실행 시 빌드된 이미지를 자동으로 `.tar` 파일로 추출하여 해당 폴더에 저장하도록 스크립트 고도화.
- **관리 스크립트**:
    - `scripts/export-images.sh`: 현재 이미지들 추출.
    - `scripts/import-images.sh`: 저장된 이미지들 도커로 로드.

## 3. 네트워크 및 포트 할당 현황
- **운영(Prod)**: HTTP(80), Dashboard(8080), DB(3306), Redis(6379), RabbitMQ(5672)
- **개발(Dev)**: HTTP(81), Dashboard(8081), DB(3307), Redis(6380), RabbitMQ(5673)

## 4. 조치 사항
- 현재 개발 환경의 DB가 가동 중이며, DB가 Healthy 상태로 전환되면 Migration 컨테이너가 실행된 후 최종적으로 App 서비스가 가동됩니다.
