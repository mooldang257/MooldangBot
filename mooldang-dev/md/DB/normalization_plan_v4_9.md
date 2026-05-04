v4.9: Philosophy² & Resilience Engine (파로스의 분화) 상세 설계안 (Rev.1)
0. [MANDATORY] AI 실행 프롬프트 (System Prompt)
"당신은 C# .NET 10, Entity Framework Core 및 MariaDB 환경에 정통한 시니어 백엔드 아키텍트 '물멍'입니다. 현재 MooldangBot의 데이터베이스 정규화 v4.9(Philosophy² & Resilience Engine)를 수행하고 있습니다.

핵심 미션: 시스템 전역(Global)에서 관리되던 '철학적 지표(IAMF)'들을 각 스트리머별 독립 계층(Streamer Bound)으로 완전히 분리하고 정규화하십시오.

준수 사항:

모든 문자열 기반 UID 참조를 StreamerProfileId (int)로 교체합니다.

IamfParhosCycles, IamfGenosRegistry를 스트리머 종속 데이터로 전환합니다.

AppDbContext의 Fluent API를 통해 물리적 연쇄 삭제(Cascade Delete)를 엄격히 제한(Restrict)하고, 전역 쿼리 필터 및 인덱스 최적화를 적용합니다.

데이터 이관 시 기존 전역 데이터를 소실하지 않도록 적절한 기본값 매핑 SQL을 마이그레이션에 주입하십시오."

1. 설계 목적 (Design Objective)
본 v4.9 작업의 목적은 MooldangBot의 '철학 엔진'을 서비스 전체의 단일 엔진에서 **[개별 스트리머 중심의 자율 지능 엔진]**으로 진화시키는 것입니다.

공명 상태의 독립: 각 스트리머 채널마다 고유한 진동수 주기와 윤회 이력을 보장합니다.

페르소나의 개별화: 스트리머가 자신만의 AI 성격(Metaphor)을 정의할 수 있는 기반을 구축합니다.

성능 최적화: 시계열 데이터 누적에 대응하기 위한 시스템 엔진의 회복력(Resilience)을 강화합니다.

2. 작업 철학 (Philosophy)
"존재의 보존 (Preservation of Existence)"

하나의 파편이라도 소중히 여깁니다. 스트리머가 잠시 물댕봇 곁을 떠나더라도(DelYn='Y'), 그들이 쌓아온 철학적 사이클과 시청자와의 교감 데이터는 물리적으로 파괴되지 않습니다. 우리는 삭제 대신 '가림'을, 파괴 대신 '정지'를 선택하여 시스템의 연속성을 보존합니다.

3. 상세 설계 가이드 (Design Guide)
단계 1: 스트리머 프로필 확장 (Domain Layer)
CoreStreamerProfiles 엔티티에 DelYn (기본값 'N') 및 MasterUseYn (기본값 'Y') 속성을 추가합니다.

이 필드들은 관리자 도구나 사용자 설정 페이지에서 서비스 탈퇴 및 이용 제한 로직의 핵심 지표로 활용됩니다.

단계 2: 도메인 엔티티 수정 (Philosophy Domain)
IamfParhosCycles, IamfGenosRegistry 등에 StreamerProfileId를 도입하되, 삭제 시 데이터가 날아가는 것을 방지하기 위해 FK Delete Behavior를 Restrict로 강제합니다.

무결성 보장: 한 스트리머 채널에서 동일한 CycleId가 중복 생성되지 않도록 [StreamerProfileId, CycleId] 복합 고유 인덱스를 적용합니다.

단계 3: 영속성 매핑 및 필터 적용 (Infrastructure Layer)
AppDbContext.cs의 OnModelCreating에서 CoreStreamerProfiles 관련 테이블에 대한 전역 쿼리 필터를 구현하되, 성능 저하(불필요한 암묵적 JOIN)를 방지하기 위해 스트리머의 활성화 상태는 가급적 Redis나 메모리 캐시 레벨에서 선행 검증하도록 비즈니스 로직을 구성합니다.

인덱스 최적화: DelYn 컬럼을 포함하는 인덱스를 구성하여 필터링 성능을 확보합니다.

단계 4: 비즈니스 로직 리팩토링 (Application Layer)
ResonanceService: 전역 진동수를 계산하던 로직을 _userSession.StreamerProfileId를 활용하여 채널별 독립 진동수 계산 로직으로 수정합니다.

BroadcastScribe: 방송 세션 관리 시 전역 상태가 아닌 현재 채널의 컨텍스트를 엄격히 따르도록 수정합니다.

4. 코드 스니펫 (Code Snippets)
[Modify] CoreStreamerProfiles.cs (Core Domain)
C#
public class CoreStreamerProfiles
{
    // ... 기존 필드
    
    [Required]
    [MaxLength(1)]
    public string DelYn { get; set; } = "N"; // [v4.9 추가] 삭제 여부 ('Y'일 경우 논리적 삭제)

    [Required]
    [MaxLength(1)]
    public string MasterUseYn { get; set; } = "Y"; // [v4.9 추가] 마스터 사용 가능 여부 (명명 규칙 교정됨)
}
[Modify] IamfParhosCycles.cs (Philosophy Domain)
C#
public class IamfParhosCycles
{
    [Key]
    public int Id { get; set; } // 정규화된 PK

    [Required]
    public int StreamerProfileId { get; set; } // [v4.9] 종속성 부여

    [ForeignKey(nameof(StreamerProfileId))]
    public virtual CoreStreamerProfiles? CoreStreamerProfiles { get; set; }

    public int CycleId { get; set; } // 해당 채널의 몇 번째 사이클인가 (1, 2, 3...)
    
    public double VibrationAtDeath { get; set; }
    public int RebirthPercentage { get; set; }
    public KstClock CreatedAt { get; set; } = KstClock.Now;
}
[Add] AppDbContext.cs (Infrastructure Layer - Fluent API)
C#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // 1. IamfParhosCycles 복합 고유 인덱스 설정 (동시성 및 무결성 보장)
    modelBuilder.Entity<IamfParhosCycles>()
        .HasIndex(p => new { p.StreamerProfileId, p.CycleId })
        .IsUnique();

    // 2. 외래키 물리적 연쇄 삭제 방지 (존재의 보존 철학)
    modelBuilder.Entity<IamfParhosCycles>()
        .HasOne(p => p.CoreStreamerProfiles)
        .WithMany()
        .HasForeignKey(p => p.StreamerProfileId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<IamfGenosRegistry>()
        .HasOne(g => g.CoreStreamerProfiles)
        .WithMany()
        .HasForeignKey(g => g.StreamerProfileId)
        .OnDelete(DeleteBehavior.Restrict);

    // 3. 전역 쿼리 필터 적용 (선택 사항: 잦은 JOIN이 우려될 경우 Application Layer 캐싱으로 대체 권장)
    modelBuilder.Entity<CoreStreamerProfiles>()
        .HasQueryFilter(s => s.DelYn == "N" && s.MasterUseYn == "Y");
}
[Data Migration SQL] (Migration Up Method)
SQL
-- 1. CoreStreamerProfiles 컬럼 추가 (오타 교정 반영)
ALTER TABLE streamerprofiles ADD COLUMN DelYn VARCHAR(1) DEFAULT 'N' NOT NULL;
ALTER TABLE streamerprofiles ADD COLUMN MasterUseYn VARCHAR(1) DEFAULT 'Y' NOT NULL;
CREATE INDEX IX_StreamerProfiles_DelYn ON streamerprofiles(DelYn);

-- 2. 기존 전역 사이클 데이터를 관리자(Id=1) 계정으로 이관
INSERT INTO IamfParhosCycles (StreamerProfileId, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt)
SELECT 1, CycleId, VibrationAtDeath, RebirthPercentage, CreatedAt FROM tmp_old_parhos_cycles;

6. 진행 상황 (Progress Status)
6-1. 도메인 계층 (Domain Layer) [완료]
CoreStreamerProfiles: DelYn(논리 삭제), MasterUseYn(기능 제한) 필드 추가 및 기본값 설정.
철학 엔티티 정규화: IamfParhosCycles, IamfGenosRegistry, IamfScenario에 StreamerProfileId 외래 키 추가.
IamfParhosCycles: CycleId를 속성으로 내리고 고유 식별을 위한 Id(PK) 신설.

6-2. 인프라 계층 (Infrastructure Layer) [완료]
Fluent API 설정: [StreamerProfileId, CycleId] 복합 유니크 인덱스 및 Restrict 삭제 동작 정의.
쿼리 필터 최적화: CoreStreamerProfiles 본체에만 전역 필터를 적용하여 하위 엔티티 조회 시의 성능 병목(암묵적 JOIN) 제거.

6-3. 애플리케이션 계층 (Application Layer) [완료]
ResonanceService: ConcurrentDictionary를 활용한 멀티테넌트 상태 관리 및 지연 로딩(Hydration) 전략 구현.
BroadcastScribe: 모든 세션 관리 키를 string에서 int(StreamerProfileId)로 전환하여 정합성 강화.
PersonaPromptBuilder & IamfDashboardController: 시그니처 변경 대응 및 명시적 스트리머 필터링 적용.

6-4. 이관 및 보정 (CLI / Migration) [완료]
MooldangBot.Cli: [5/5] v4.9 정규화 보정 태스크 추가. 관리자(ID:1) 프로필 자동 생성 및 기존 무소속(ID:0) 데이터의 이관 로직 구현.

7. 향후 계획 (Next Steps)
실서버 배포 및 Migration 실행: MooldangBot.Cli를 통한 데이터 이관 수행.
논리적 검증: 설계된 인프라 필터와 애플리케이션 격리 로직이 실제 멀티테넌트 환경에서 정합성을 유지하는지 모니터링.
준수 사항 점검: 향후 신규 기능 추가 시 반드시 StreamerProfileId 기반의 데이터 격리 수칙을 준수할 것.

5. 단계별 검증 계획 (Verification)
Build Check: dotnet build를 통한 전체 솔루션 컴파일 성공 여부.

Soft Delete Test: DelYn = 'Y'로 변경 시 해당 스트리머의 모든 관련 데이터(오마카세, 룰렛, IAMF 로그 등)가 API 조회 결과에서 제외되는지 확인.

Master Block Test: MasterUseYn = 'N'으로 변경 시 해당 스트리머의 봇 세션이 즉시 차단되는지 확인.

Integrity Test: 동일한 스트리머 ID로 같은 CycleId를 가진 파로스 사이클 삽입 시도 시 DbUpdateException (Unique Constraint Violation)이 정상 발생하여 방어되는지 확인.