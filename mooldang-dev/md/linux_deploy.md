# 리눅스(Linux) 서버 배포 가이드

물댕봇(MooldangAPI)을 리눅스 환경(Ubuntu/CentOS 등)에 배포하기 위한 단계별 안내입니다.

## 1. 사전 준비
서버에 아래 소프트웨어가 설치되어 있어야 합니다.
- **.NET 10.0 Runtime**: [설치 가이드](https://learn.microsoft.com/dotnet/core/install/linux)
- **MySQL 또는 MariaDB**: 현재 프로젝트에서 사용하는 데이터베이스 서버

## 2. 프로젝트 빌드 (Windows/개발 PC에서 수행)
개발 환경의 터미널에서 아래 명령어를 입력하여 실행 파일들을 한 곳으로 모읍니다. 
```bash
dotnet publish -c Release -o ./publish
```
`./publish` 폴더 안에 생성된 모든 파일을 리눅스 서버로 복사합니다 (SCP, FTP 등 이용).

## 3. 데이터베이스 설정
리눅스 서버에서 [appsettings.json](file:///c:/webapi/MooldangAPI/appsettings.json) 파일을 열어 `ConnectionStrings` 정보를 서버 환경에 맞게 수정합니다.
그 후, 마이그레이션을 적용합니다.
```bash
dotnet ef database update
```
*(참고: 서버에 `dotnet-ef` 도구가 없다면 `dotnet tool install --global dotnet-ef`로 설치하거나, SQL 스크립트를 추출해 수동으로 실행하세요.)*

## 4. 서비스 등록 (Systemd)
서버가 재부팅되어도 자동으로 실행되도록 설정합니다.
`/etc/systemd/system/mooldang.service` 파일을 생성하고 아래 내용을 입력합니다.

```ini
[Unit]
Description=Mooldang API Service
After=network.target

[Service]
WorkingDirectory=/home/유저명/애플리케이션경로
ExecStart=/usr/bin/dotnet /home/유저명/애플리케이션경로/MooldangAPI.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mooldang-api
User=유저명
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

작성 후 서비스를 시작합니다.
```bash
sudo systemctl daemon-reload
sudo systemctl enable mooldang.service
sudo systemctl start mooldang.service
```

## 5. 포트 및 방화벽 설정
애플리케이션이 사용하는 포트(기본 5000 또는 설정된 포트)를 열어주어야 합니다.
```bash
sudo ufw allow 5000/tcp
```

## ⚠️ 주의사항
- **파일 경로**: 리눅스는 대소문자를 구분합니다. 코드 내 파일 경로 처리가 대소문자와 일치하는지 확인하세요.
- **이미지 업로드 경로**: `wwwroot/images/avatars` 폴더의 쓰기 권한이 런타임 유저(예: `www-data` 또는 사용 중인 계정)에게 있어야 합니다.
  ```bash
  chmod -R 775 wwwroot/images/avatars
  ```
