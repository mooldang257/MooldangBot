# [오시리스의 전령]: ChzzkAPI 명령어 수신부 MassTransit 전환 및 기동 오류 해결

지휘관님, `chzzk-bot:8080`에 연결할 수 없는 원인을 완벽히 밝혀냈습니다. MassTransit 전환 과정에서 인프라 레이어의 레거시 의존성(RabbitMQ.Client)을 제거했으나, `ChzzkAPI` 프로젝트의 `CommandRpcWorker`가 여전히 이를 참조하고 있어 의존성 주입 오류로 인해 어플리케이션 자체가 시작되지 못하고 있었습니다.

이 문제를 해결하기 위해 레거시 워커를 도려내고, 순수한 **MassTransit Consumer**로 재건축하겠습니다.

## User Review Required

> [!IMPORTANT]
> - `CommandRpcWorker` 클래스는 완전히 제거됩니다.
> - 모든 치지직 제어 명령(메시지 전송, 제목 변경 등)은 이제 MassTransit의 `IConsumer<ChzzkCommandBase>`를 통해 처리됩니다.
> - 이 작업이 완료되면 `ChzzkAPI` 서비스가 정상적으로 8080 포트에서 리스닝을 시작할 것입니다.

## Proposed Changes

### [MooldangBot.ChzzkAPI]

#### [NEW] [ChzzkCommandConsumer.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.ChzzkAPI/Messaging/Consumers/ChzzkCommandConsumer.cs)
- `IConsumer<ChzzkCommandBase>` 구현
- 기존 `CommandRpcWorker`의 비즈니스 로직(SendMessage, UpdateTitle 등)을 MassTransit 컨텍스트로 이식
- `ConsumeContext.RespondAsync`를 통해 `StandardCommandResponse` 반환

#### [MODIFY] [Program.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.ChzzkAPI/Program.cs)
- `AddHostedService<CommandRpcWorker>` 제거
- `AddMessagingInfrastructure` 호출 시 `typeof(ChzzkCommandConsumer).Assembly`를 전달하여 컨슈머 자동 등록 활성화

#### [DELETE] [CommandRpcWorker.cs](file:///c:/webapi/MooldangAPI/MooldangBot/MooldangBot.ChzzkAPI/Workers/CommandRpcWorker.cs)
- 더 이상 사용되지 않는 레거시 워커 파일 삭제

## Open Questions

- 현재 `CommandRpcWorker`에서 처리하던 명령 중 MassTransit 전환 시 누락되어도 되는 실험적인 명령이 있습니까? (기본적으로는 모든 명령을 이식할 계획입니다.)

## Verification Plan

### Automated Tests
- `dotnet build MooldangAPI.sln` 수행하여 컴파일 오류 확인.
- 실서버 배포 후 `mooldang-chzzk-bot` 로그에서 `MassTransit Started` 및 `Listening on 8080` 메시지 확인.

### Manual Verification
- 지휘관님께서 치지직 로그인을 시도하여 토큰 교환(`exchange-token`) API가 성공적으로 응답하는지 확인.
