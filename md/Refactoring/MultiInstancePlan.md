[Final Plan] 다중 인스턴스 배포 및 보안 강화 리팩토링 계획
물당님, 소스 코드와 민감 정보를 완전히 분리하여 보안 사고를 예방하고, 리눅스 서버 환경에서 직접 인스턴스를 제어하기 위한 최적화된 로드맵입니다.

📌 1. 개요 (Background)
기존의 하드코딩된 설정 방식에서 벗어나, 소스 코드는 GitHub에 안전하게 공유하고, 실제 서비스 운영에 필요한 **민감 데이터(DB 명칭, 비밀번호, API 키)**는 오직 리눅스 서버 내부에서만 관리하도록 설계합니다. 이를 통해 동일 서버 내에서 www와 bot 인스턴스를 완벽하게 격리하여 운영합니다.

🏗️ 2. 주요 리팩토링 및 보안 강화 대상
[x] 2.1 보안 및 비밀 정보 격리 (Zero-Git Policy)
현재: 일부 설정값이 소스 코드나 appsettings.json에 포함되어 Git 서버에 노출될 위험 존재.

개선: 모든 민감 정보를 리눅스 서버 내의 독립된 .env 파일 또는 시스템 환경 변수로 이전.

조치: .gitignore를 강화하여 설정 파일이 절대 Git에 올라가지 않도록 차단.

[x] 2.2 포트 및 도메인 동적 할당
개선: 실행 시 인자(--env=...)를 통해 각 인스턴스가 점유할 포트와 도메인을 결정.

효과: 하나의 소스 코드로 5000(운영), 5001(테스트) 등 여러 포트 동시 가동.

[x] 2.3 데이터베이스(DB) 및 세션 격리
개선: .env 파일에 각 인스턴스 전용 DB 이름과 보안 쿠키 이름을 정의.

효과: mooldang_prod와 mooldang_test DB를 분리하여 테스트 중 운영 데이터 훼손 방지.

🛠️ 3. 상세 구현 및 보안 구성
3.1 Git 서버 보안 설정 (.gitignore)
프로젝트 루트에서 아래 파일들을 Git 추적 대상에서 제외합니다.

Plaintext
# .gitignore 파일 내용
.env
.env.*
appsettings.json
appsettings.Development.json
secrets/
3.2 리눅스 서버 내 환경 설정 (Manual Input)
서버의 특정 경로(예: /home/mooldang/secrets/)에 각 인스턴스별 설정 파일을 직접 생성합니다. (이 과정은 SSH로 서버에 접속해서 직접 수행합니다)

Bash
# 예: bot.mooldang.store용 설정 파일 (.env.bot)
DB_NAME=mooldang_test
DB_USER=mooldang_admin
DB_PASSWORD=서버에서_직접_입력한_비번
ASPNETCORE_URLS=http://*:5001
AUTH_COOKIE_NAME=mooldang_bot_session
CHZZK_CLIENT_SECRET=애플리케이션_시크릿_키
### 3.3 Program.cs 수정 (동적 로드 로직)
서버에서 입력한 값을 앱이 읽어오도록 구성합니다. 프로젝트의 실제 컨텍스트 이름인 `AppDbContext`를 사용합니다.

```csharp
// 1. 실행 인자에서 설정 파일 경로 추출
var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

// 2. 서버 로컬에 있는 설정 파일 로드 (Git에는 없음)
if (File.Exists(envPath)) {
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(); // 시스템 환경 변수 통합

### 3.4 ForwardedHeaders 보안 설정 (Cloudflare 대응)
터널을 통해 `bot.mooldang.store`로 접속 시 호스트 정보가 유실되지 않도록 `Program.cs`에 아래 설정을 반드시 포함합니다.

```csharp
using Microsoft.AspNetCore.HttpOverrides;

// ... builder 생성 이후 ...
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    // Cloudflare 터널은 내부망에서 오므로 보안을 위해 네트워크 제한을 풉니다.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// 💡 인증 및 라우팅 미들웨어보다 앞에 위치해야 합니다.
app.UseForwardedHeaders();
```

### 3.5 리눅스 서버 보안 권한 설정 (Operational Security)
서버의 `secrets` 폴더와 설정 파일은 오직 실행 계정만 읽을 수 있도록 권한을 조여주는 것이 안전합니다.

```bash
# 폴더 생성 및 권한 설정 (계정 주인만 접근 가능)
mkdir -p /home/mooldang/secrets
chmod 700 /home/mooldang/secrets

# .env.* 파일 권한 설정 (다른 유저의 읽기/쓰기 차단)
chmod 600 /home/mooldang/secrets/.env.*
```
🚀 4. 배포 및 운영 시나리오 (Systemd)
리눅스 서버(Ubuntu 24.04)에서 각 인스턴스를 서비스로 등록할 때 설정을 다르게 주입합니다.

운영용 (www.mooldang.store)
실행 명령: dotnet MooldangBot.dll --env=/home/mooldang/secrets/.env.prod

터널 설정: Cloudflare Tunnel에서 5000 포트로 연결.

리팩토링 테스트용 (bot.mooldang.store)
실행 명령: dotnet MooldangBot.dll --env=/home/mooldang/secrets/.env.bot

터널 설정: Cloudflare Tunnel에서 5001 포트로 연결.

🛡️ 5. 기대 효과
완벽한 보안: GitHub에는 순수 로직만 남고, 모든 핵심 비밀 정보는 물당님의 i5-12400 서버 안에만 안전하게 보관됩니다.

사고 방지: 테스트 DB와 운영 DB가 물리적으로 분리되어 리팩토링 테스트를 마음껏 진행할 수 있습니다.

간편한 확장: 새로운 도메인이 필요하면 서버에서 .env 파일 하나만 더 만들고 서비스 등록만 하면 끝납니다.


물당님, 추가해 주신 **터널 정합성 설정(ForwardedHeaders)**과 **서버 보안 권한 전략(chmod)**을 계획에 모두 통합했습니다. 이제 GitHub에는 안전한 로직만 남고, 모든 핵심 비밀 정보는 물당님의 서버 안에서 철저히 격리되어 보호될 것입니다.

최종 검토 후, 구현(Execution) 단계로 진행할 준비가 되셨다면 말씀해 주세요! :)