# MooldangBot 운영 환경 배포 가이드 (Phase 1.5)

프로젝트 구조 단순화(Phase 1) 이후 운영 환경에 안전하게 배포하기 위한 체크리스트 및 가이드입니다.

## 1. 필수 환경 변수 (`.env`) 설정
운영 환경의 `.env` 파일(또는 Docker Secrets)에서 아래 항목들이 **Docker 내부망 주소**를 가리키고 있는지 확인하세요.

| 키 | 로컬 설정 (Host) | 운영 설정 (Docker) | 비고 |
| :--- | :--- | :--- | :--- |
| `DB_HOST` | `localhost` | `db` | |
| `REDIS_URL` | `localhost:6379` | `redis:6379` | |
| `RABBITMQ_HOST` | `localhost` | `rabbitmq` | |
| `LOKI_URL` | `http://localhost:3100` | `http://loki:3100` | |
| `CONNECTIONSTRINGS__DEFAULTCONNECTION` | `Server=localhost;...` | `Server=db;...` | 가장 중요 |

## 2. 데이터베이스 마이그레이션 적용
운영 환경 배포 후 데이터베이스 스키마를 최신화해야 합니다.

### 방법 A: Docker를 통한 자동 마이그레이션 (추천)
`docker-compose.yml`의 `migration` 서비스가 정상 기동되면 자동으로 적용됩니다.
```bash
docker-compose up migration
```

### 방법 B: 수동 명령 실행
이미 컨테이너가 떠 있는 상태에서 수동으로 적용하려면:
```bash
docker-compose exec app dotnet /app/cli/MooldangBot.Cli.dll --migrate
```

## 3. 포트 변경 사항
- **Adminer**: 8080 포트 충돌 방지를 위해 **8888**로 변경되었습니다.
- 접속 주소: `http://<서버IP>:8888`

## 4. 빌드 및 배포 명령어
```bash
# 이미지 빌드 (불필요한 프로젝트 제외됨)
docker-compose build

# 시스템 전체 기동
docker-compose up -d
```

> [!WARNING]
> 운영 서버 배포 전 반드시 로컬에서 `docker-compose build`가 성공하는지 최종 확인하십시오.
