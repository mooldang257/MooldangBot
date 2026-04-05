# 01. Database Convention

## 1. 개요
이 문서는 MooldangBot의 MariaDB와 EF Core를 다룰 때 반드시 지켜야 할 기술적 규칙을 정의합니다. 특히 리눅스 서버(Ubuntu) 환경에서의 완벽한 호환성과 대규모 동시성 처리를 보장하는 것을 최우선 목표로 합니다.

## 2. 네이밍 규칙 (Naming Rules)
* **[Rule 1]** 모든 **테이블 및 컬럼명**은 반드시 **소문자 스네이크 케이스(`snake_case`)**를 사용한다.
  * 리눅스 MariaDB의 대소문자 구분(Case-sensitivity) 이슈를 원천 차단하기 위함입니다.
  * **[1.1 Raw SQL 주의]** Dapper나 ADO.NET을 활용한 **직접 쿼리 작성 시에도 반드시 소문자 컬럼명을 사용**해야 합니다. PascalCase 사용 시 도커 환경에서 `Unknown column` 오류가 발생합니다.
  * ❌ `SELECT StreamerProfileId FROM ...`
  * ✅ `SELECT streamer_profile_id FROM ...`
* **[Rule 2]** 데이터베이스 객체 이름에 공백이나 특수문자 사용을 금지하며, 의미가 명확한 영문 단어를 조합한다.

## 3. 문자셋 및 정렬 규칙 (Character Set & Collation)
* **[Rule 3]** 모든 문자열 데이터는 전역적으로 **`utf8mb4_unicode_ci`** 정렬 규칙을 사용한다.
  * 이는 `AppDbContext.OnModelCreating`에서 전역적으로 강제되며, 이모지(Emoji) 및 다국어 지원을 완벽히 보장합니다.
  * ❌ `utf8mb4_general_ci` (정확도 낮음)
  * ✅ `utf8mb4_unicode_ci` (비즈니스 로직 및 정렬 정확도 높음)

## 4. EF Core 및 마이그레이션 정책
* **[Rule 4]** 마이그레이션 파일이 비대해지거나 구조적 충돌이 발생할 경우, **압착(Squash)** 작업을 통해 단일 `InitialCreate` 파일로 통합하여 배포 안정성을 유지한다.
* **[Rule 5]** `AppDbContext` 내의 `ToTable()` 호출 시 반드시 스네이크 케이스 명칭을 명시하여 물리 스키마와 코드 간의 정합성을 유지한다.

## 5. 데이터 영속성 및 삭제 정책 (Persistence)
* **[Rule 6]** 모든 주요 마스터 데이터는 **논리적 삭제(Soft Delete)**를 원칙으로 한다.
  * `del_yn` (Y/N) 컬럼 또는 `IsDeleted` 플래그를 활용하여 데이터 증적을 보존한다.
* **[Rule 7]** 스트리머가 삭제될 경우, 해당 스트리머와 관련된 종속 데이터(동작, 설정 등)는 `Cascade` 삭제를 통해 무결성을 유지한다.

---
**최종 갱신일**: 2026-04-02 (물멍 파트너 작성)
