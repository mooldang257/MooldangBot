# 🌊 MooldangBot & MooldangAPI

스트리머 'mooldang'을 위한 치지직 연동 노래책 및 방송 보조 도구 시스템입니다.

---

## 🚀 빠른 시작 (리눅스 서버 배포)

### 1. 초기 인프라 설정 (딱 한 번)
서버에 도커가 설치된 상태에서, 부팅 시 도커가 자동 실행되도록 설정합니다.

```bash
# 도커 서비스 활성화 및 시작
sudo systemctl enable docker
sudo systemctl start docker

# (필요시) 현재 사용자를 docker 그룹에 추가 (로그아웃 후 재접속 필요)
sudo usermod -aG docker $USER
```

### 2. 소스 코드 및 설정 준비
```bash
# 저장소 클론 (이미 되어있다면 패스)
git clone https://github.com/mooldang257/MooldangBot.git
cd MooldangBot

# 설정 파일 생성
cp .env.sample .env
nano .env  # 본인의 API 키와 비밀번호 기입
```

### 3. 원클릭 배포 실행
```bash
# 스크립트 실행 권한 부여
chmod +x deploy.sh

# 배포 시작 (Git Pull -> Build -> UP -> Migration)
./deploy.sh
```

---

## 💻 로컬 개발 환경 (Windows)

### 1. 요구 사항
*   **Visual Studio 2022** (v17.10 이상)
*   **.NET 10.0 SDK**

### 2. 실행 방법
1.  `MooldangAPI.sln` 파일을 비주얼 스튜디오로 엽니다.
2.  `MooldangBot.Api` 프로젝트를 시작 프로젝트로 설정합니다.
3.  `F5`키를 눌러 실행합니다. (`launchSettings.json`에 의해 개발 모드로 자동 시작됩니다.)

---

## 🛡️ 보안 규정 (Zero-Git Policy)
*   모든 민감 정보(비밀번호, 시크릿 키)는 절대 깃허브에 올리지 않습니다.
*   반드시 `.env` 파일이나 서버 환경 변수를 통해서만 설정합니다.
*   `.gitignore`가 자동으로 `.env` 및 `appsettings` 파일을 차단하고 있으므로 안심하십시오.

---

## 🐳 도커 주요 명령어 기반 운영
*   **로그 확인**: `docker-compose logs -f app`
*   **상태 확인**: `docker-compose ps`
*   **완전 재시작**: `docker-compose up -d --force-recreate`

---

**개발 파트너 물멍**이 정성을 담아 구축했습니다. 물댕봇과 함께 즐거운 방송 되세요! 🦾✨
