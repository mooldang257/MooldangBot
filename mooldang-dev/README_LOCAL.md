# 🌊 MooldangBot 로컬 테스트 가이드

이 문서는 로컬 환경에서 `Docker Desktop`을 사용하여 MooldangBot 전체 시스템을 구동하고 테스트하는 방법을 설명합니다. 사용자의 요청에 따라 모니터링 스택(Grafana, Loki 등)은 제외되었습니다.

## 🚀 시작하기

1. **사전 준비**:
   - [Docker Desktop](https://www.docker.com/products/docker-desktop/)이 설치되어 있고 실행 중이어야 합니다.
   - 프로젝트 루트 디렉토리(`./MooldangAPI`)에 `.env` 파일이 있는지 확인하세요.

2. **실행 방법**:
   - `start-dev.bat` 파일을 더블 클릭하거나,
   - 파워쉘에서 `.\run-local.ps1`을 실행합니다.

## 🔗 주요 서비스 접속 정보 (Localhost)

| 서비스 명 | URL / 접속 정보 | 비고 |
| :--- | :--- | :--- |
| **통합 게이트웨이 (Nginx)** | [http://localhost:8080](http://localhost:8080) | Studio, Admin, API 통합 접속 |
| **RabbitMQ 관리자** | [http://localhost:15672](http://localhost:15672) | 계정: `.env` 파일의 `RABBITMQ_USER` 참고 |
| **MariaDB** | `localhost:3306` | 계정: `mooldang` / `.env` 파일의 패스워드 참고 |

## 🛠️ 주요 스크립트 기능

- **`run-local.ps1`**: 
  - 필수 인프라(`db`, `redis`, `rabbitmq`)와 핵심 로직(`app`, `chzzk-bot`, `migration`) 및 프론트엔드(`studio`, `admin`, `overlay`) 등을 선별하여 기동합니다.
  - 기동 후 가장 중요한 **`mooldang-app` (API 서버)의 로그를 실시간으로 출력**합니다.

## ⚠️ 주의 사항

- **포트 충돌**: 로컬 PC에서 이미 3306(MySQL/MariaDB), 6379(Redis), 8080 포트를 사용 중인지 확인하세요.
- **데이터 유지**: 모든 데이터는 `./data` 폴더에 볼륨으로 저장됩니다. 데이터를 완전히 초기화하고 싶다면 `docker-compose down -v` 명령을 수동으로 실행하세요.

---
**물멍!** 안정적인 로컬 테스트 환경이 되길 바랍니다. 🐾
