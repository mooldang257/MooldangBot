# 05. Git Workflow & Versioning

## 1. 개요
MooldangBot 프로젝트의 변경 이력을 투명하게 관리하고, 안정적인 배포(Deployment)를 보장하기 위한 형상 관리 규칙을 정의합니다.

## 2. 커밋 메시지 규격 (Conventional Commits)
모든 커밋은 변경의 성격과 범위를 한눈에 알 수 있어야 합니다. **커밋 메시지 내에 [vX.X.X]와 같은 버전 번호는 기재하지 않으며, 버전 관리는 Git Tag로 분리합니다.**

❌ **Don't: 의미 없는 커밋 또는 버전 번호 중복 기재**
```bash
git commit -m "버그 수정"
git commit -m "[v4.9.0] feat(DB): 정규화 로직 적용" # [버전 번호는 태그로 관리]
```

✅ **Do: 범위(Scope)와 성격이 명시된 순수 커밋**
- 형식: `<type>(<scope>): <subject>`
- 예시: `feat(DB): 정규화 로직 및 마이그레이션 보정 완료`

| Type | 설명 |
| :--- | :--- |
| **feat** | 새로운 기능 추가 |
| **fix** | 버그 수정 |
| **docs** | 문서 수정 (guidelines 등) |
| **refactor** | 기능 변경 없는 코드 구조 개선 |
| **chore** | 빌드 설정, 의존성 관리 등 기타 작업 |

## 3. 브랜치 및 태그 전략 (Branching & Tagging)
- **main/master**: 상시 배포 가능한 안정 상태의 소스.
- **Git Tag**: 배포 시점에 `git tag v4.9.0`과 같이 버전 태그를 부여하여 관리합니다.

## 4. 버전 관리 (Semantic Versioning)
봇(Bot)의 버전은 `v[Major].[Minor].[Patch]` 형식을 따릅니다.
- **Major**: 하위 호환성이 깨지는 대규모 개편 (v6.0 등)
- **Minor**: 하위 호환성을 유지한 기능 추가 (v6.1 등)
- **Patch**: 단순 버그 수정 및 최적화 (v6.1.1 등)

## 5. 배포 자동화 및 검증 (Deployment)
배포는 반드시 `deploy.sh`를 통하며, 상황에 따라 캐시 사용 여부를 선택할 수 있습니다.

✅ **Do: 상황에 맞는 배포 스크립트 실행**
```bash
# [일반] 빠른 배포 (Docker 레이어 캐시 활용)
./deploy.sh

# [클린] 대규모 업데이트 시 정합성 보장을 위한 클린 빌드 배포
./deploy.sh --clean
```

❌ **Don't: 서버에서 직접 코드 수정**
- 모든 변경 사항은 로컬에서 테스트 후 Git을 통해 배포되어야 합니다.

---
**아키텍트 검토 요청 사항**:
- 현재 사용 중인 `[vX.X.X]` 형태의 커밋 Prefix와 Conventional Commits의 `feat(Scope):` 방식을 혼용할지, 아니면 하나로 통일할지 아키텍트님의 최종 가이드를 부탁드립니다.
- 배포 시 `docker-compose build --no-cache` 옵션 사용 여부(속도 vs 정항성)에 대한 의견을 주시면 반영하겠습니다.
