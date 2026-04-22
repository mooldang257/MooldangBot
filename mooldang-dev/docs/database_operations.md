# MooldangBot Database Operations

이 문서는 MooldangBot의 데이터베이스 유지보수 및 수동 데이터 조작이 필요한 경우를 위한 가이드입니다. 

> [!CAUTION]
> 아래의 `TRUNCATE` 명령어는 테이블의 모든 데이터를 삭제합니다. 실행 전 반드시 백업을 수행하고, 테스트 환경에서 먼저 검증하십시오.

## 1. 초기화 및 리프레시 (TRUNCATE)

데이터베이스 초기 동기화나 개발 환경 리셋이 필요한 경우 사용합니다.

```sql
SET FOREIGN_KEY_CHECKS = 0;

-- 주요 설정 및 로그 테이블 초기화
TRUNCATE TABLE unified_commands;
TRUNCATE TABLE streamer_profiles;
TRUNCATE TABLE streamer_managers;
TRUNCATE TABLE song_queues;
TRUNCATE TABLE song_list_sessions;
TRUNCATE TABLE streamer_omakases;
TRUNCATE TABLE roulette_logs;
TRUNCATE TABLE viewer_profiles;

SET FOREIGN_KEY_CHECKS = 1;
```

## 2. 수동 SQL 접속 (Docker 환경)

도커 컨테이너 내부의 MariaDB CLI에 접속하는 방법입니다.

```bash
# mooldang-db 컨테이너 접속
docker exec -it mooldang-db mysql -u mooldang -p MooldangBot
```

## 3. 유의 사항
- 모든 테이블명은 **소문자 스네이크 케이스(`snake_case`)**를 준수해야 합니다.
- 데이터 삭제 후에는 `MooldangBot.Cli`를 통한 시딩(`Seeding`)을 권장합니다.
