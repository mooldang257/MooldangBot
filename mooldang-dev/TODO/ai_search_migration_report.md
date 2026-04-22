# [보고서] 송북 AI 검색 고도화 및 하이브리드 엔진 마이그레이션 현황

지휘관님, 이동하시기 전까지 작업된 내용과 향후 이어서 하셔야 할 내용을 정리해 드립니다.

## 1. 현재까지 완료된 작업 (Done)

- **AI 실전 배치**: 
    - 전달해주신 Gemini API 키를 `.env`에 등록하고 **실제 AI 신경망(`GeminiLlmService`)을 활성화**했습니다.
    - 이제 봇은 가상의 응답이 아닌 실제 구글 AI와 통신합니다.
- **데이터 구조 설계**:
    - `Streamer_SongLibrary` 엔티티에 줄임말(`Alias`), 초성(`TitleChosung`), 의미 좌표(`TitleVector`) 필드를 추가했습니다.
- **검색 엔진 고도화**:
    - `SongBookRepository`에 **하이브리드 검색(제목+별명+초성+벡터)** 로직을 구현했습니다.
    - 오타가 있어도 의미가 비슷하면 찾아낼 수 있는 기반(Vector Search)을 마련했습니다.
- **유틸리티**:
    - 한글 제목에서 초성(ㅇㄹㄴㅇㅈㄱ)을 자동으로 뽑아내는 `ChosungUtility`를 구현했습니다.

## 2. 발생 중인 이슈 및 긴급 조치 필요 (Blockers)

- **빌드 오류**: `ISongBookDbContext` 인터페이스에 신규 필드(`StreamerSongLibraries`) 정의가 누락되어 빌드가 실패한 상태입니다.
- **DB 동기화**: 엔티티 구조가 바뀌었으나 아직 데이터베이스에 실제 컬럼이 생성되지 않았습니다. (`dotnet ef migrations add` 필요)

## 3. 향후 작업 로드맵 (Remaining Tasks)

- [ ] **인터페이스 수정**: `ISongBookDbContext.cs`에 `DbSet<Streamer_SongLibrary> StreamerSongLibraries` 속성 추가.
- [ ] **DB 마이그레이션**: 신규 컬럼 반영을 위한 마이그레이션 생성 및 적용.
- [ ] **AI 메타데이터 자동 생성 연동**:
    - 노래가 등록되거나 신청될 때 AI가 자동으로 별명("우언죽")과 벡터를 생성하여 저장하는 로직 연결.
- [ ] **최종 테스트**: 실제 채팅창에서 "!신청 우언죽", "!신청 우리는언잰가죽어요"가 정상 작동하는지 확인.

## 4. 참고 사항

> [!TIP]
> `MooldangBot.Infrastructure/ApiClients/Philosophy/GeminiLlmService.cs`에 오타를 잡아내는 `GetEmbeddingAsync` 기능까지 모두 구현해 두었습니다. 
> 
> 지휘관님이 다시 돌아오시면 **DB 마이그레이션**과 **인터페이스 누락 수정**만 먼저 처리하시면 즉시 AI 엔진을 가동하실 수 있습니다.

안전하게 이동하시고, 돌아오시면 이어서 작업을 완료해 드리겠습니다! 💦
