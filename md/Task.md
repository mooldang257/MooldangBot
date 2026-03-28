# 작업 현황 (Task.md)

## [x] 문서 및 환경 설정
- [x] 전용 문서 체계 수립 (Task/Plan/Remark)
- [x] `Research.md`에 현재 진행 상황 기록
- [x] `상세Plan.md` 구체화 (테이블 포함)

## [x] 통합 명령어 마스터 데이터화 (v1.2)
- [x] 1단계: Domain Layer (엔티티 및 DTO 설계)
- [x] 2단계: Infrastructure Layer (DB 구성 및 캐시 서비스)
- [x] 3단계: Presentation Layer (API 엔드포인트)

## [x] 커서 기반 페이징 전환 (v1.3)
- [x] `GetUnifiedCommands`| 커서 기반 페이징 전환 | 2026-03-28 | v1.3 Keyset Paging 성능 최적화 완료 |
| DB 인덱스 스캔 최적화 | 2026-03-28 | LINQ ToLower 제거 (SARGability 개선) |
| 인풋 페이징 전환 | 2026-03-28 | v1.5 페이지 직접 입력 UI 및 백엔드 복원 완료 |
| DB 스키마 업데이트 | 2026-03-28 | 마스터 데이터 테이블 및 시딩 반영 완료 |

## [x] 인풋 페이징(Input Paging) 고도화 (v1.5)
- [x] 관련 폴더 생성 및 상세 상세Plan.md 작성
- [x] Backend: 오프셋 기반 인풋 페이징 API 복원
- [x] Frontend: 인풋 UI 연동 및 점프 기능 구현

## [x] 통합 명령어 수정(Edit) 기능 구현 (v1.6)
- [x] Backend: `SaveUnifiedCommandRequest` DTO 추가
- [x] Backend: `POST /api/commands/unified/{chzzkUid}` (Upsert) 구현
- [x] Frontend: 명령어 목록에 '수정' 버튼 추가 및 전역 변수 관리
- [x] Frontend: `editUnifiedCommand` 및 `saveUnifiedCommand` 고도화

## [x] 시스템 메시지(Fixed) 최적화 및 송리스트 이동 (v1.7)
- [x] Backend: `CommandCategory` Enum 재편 및 전역 참조 수정
- [x] Backend: `Master_CommandFeature` 시딩 데이터 전면 재편 (9종 기획 반영)
- [x] Frontend: `commands.html` UI 방어 로직 (비용 잠금) 실장
- [x] Frontend: 카테고리 색상 및 라벨링 고도화

## [x] 데이터베이스 스키마 마이그레이션
- [x] EF Core 신규 마이그레이션 생성 (`AddMasterCommandTables_v12`)
- [x] DB 최신 상태 업데이트 (`database update`)
- [x] 시딩 데이터(Seeding) 정상 적재 확인
