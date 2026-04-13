using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Features.Roulette;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.State;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Application.Features.Roulette.Notifications;
using MediatR;
using NSubstitute;
using Xunit;
using StackExchange.Redis;
using RedLockNet;

namespace MooldangBot.StressTests;

public class RouletteConcurrencyTests : IDisposable
{
    private readonly ILogger<RouletteService> _mockLogger = Substitute.For<ILogger<RouletteService>>();
    private readonly IServiceScopeFactory _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IMediator _mockMediator = Substitute.For<IMediator>();
    private readonly OverlayState _mockOverlayState = Substitute.For<OverlayState>();
    private readonly IRouletteLockProvider _lockProvider;
    private readonly RouletteState _rouletteState = new();
    private readonly List<SqliteConnection> _connections = new();

    public RouletteConcurrencyTests()
    {
        // 실전과 유사하게 동작하도록 로컬 락 기능을 가진 진짜 프로바이더 사용 (Redis는 커넥션 실패 상태로 Mocking)
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(false); // 로컬 폴백 유도
        var mockLockFactory = Substitute.For<IDistributedLockFactory>();
        
        _lockProvider = new MooldangBot.Infrastructure.Security.RouletteLockProvider(
            mockLockFactory, 
            mockRedis, 
            Substitute.For<ILogger<MooldangBot.Infrastructure.Security.RouletteLockProvider>>());
    }

    public void Dispose()
    {
        foreach (var conn in _connections)
        {
            conn.Close();
            conn.Dispose();
        }
    }

    private (AppDbContext db, SqliteConnection conn) CreateScopedDatabase()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        _connections.Add(conn);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        var mockUserSession = Substitute.For<IUserSession>();
        var mockProtectionProvider = Substitute.For<IDataProtectionProvider>();
        var mockProtector = Substitute.For<IDataProtector>();
        mockProtectionProvider.CreateProtector(Arg.Any<string>()).Returns(mockProtector);

        var db = new AppDbContext(options, mockUserSession, mockProtectionProvider);
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private async Task SeedDataAsync(AppDbContext db, string chzzkUid)
    {
        var streamer = new StreamerProfile { ChzzkUid = chzzkUid, ChannelName = "TestStreamer" };
        db.StreamerProfiles.Add(streamer);
        await db.SaveChangesAsync();

        var roulette = new Roulette 
        { 
            StreamerProfileId = streamer.Id, 
            Name = "TestRoulette",
            Items = new List<RouletteItem> 
            { 
                new RouletteItem { ItemName = "Item1", Probability = 100, IsActive = true } 
            } 
        };
        db.Roulettes.Add(roulette);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SpinRoulette_Should_Wait_And_Acquire_Lock_For_Same_Streamer()
    {
        var (db, _) = CreateScopedDatabase();
        var chzzkUid = "streamer1";
        await SeedDataAsync(db, chzzkUid);

        var service = new TestableRouletteService(db, _mockScopeFactory, _rouletteState, _mockLogger, _mockMediator, _mockOverlayState, _lockProvider);
        service.DelayExecuteSpinLogic = true;

        var task1 = Task.Run(() => service.SpinRouletteAsync(chzzkUid, 1, "viewer1"));
        await Task.Delay(200); 

        var task2 = service.SpinRouletteAsync(chzzkUid, 1, "viewer2");

        task1.IsCompleted.Should().BeFalse();
        
        var delayTask = Task.Delay(500);
        var completedTask = await Task.WhenAny(task2, delayTask);
        completedTask.Should().Be(delayTask);

        service.ReleaseDelay();
        await task1;
        await task2;
    }

    [Fact]
    public async Task SpinRoulette_Should_Timeout_After_10s_For_Congested_Streamer()
    {
        var (db, _) = CreateScopedDatabase();
        var chzzkUid = "streamer_busy";
        await SeedDataAsync(db, chzzkUid);

        var service = new TestableRouletteService(db, _mockScopeFactory, _rouletteState, _mockLogger, _mockMediator, _mockOverlayState, _lockProvider);
        service.DelayExecuteSpinLogic = true;
        
        var task1 = Task.Run(() => service.SpinRouletteAsync(chzzkUid, 1, "viewer1"));
        await Task.Delay(500);

        var task2 = service.SpinRouletteAsync(chzzkUid, 1, "viewer2");
        
        var result2 = await task2;
        result2.Should().BeNull();

        service.ReleaseDelay();
        await task1;
    }

    [Fact]
    public async Task SpinRoulette_Should_Run_Parallel_For_Different_Streamers()
    {
        // ⭐ 이 테스트는 '진정한 병렬성'을 검증하므로, 
        // 락이 제대로 작동한다면 두 스트리머의 작업이 동시 실행되며 
        // 공유 자원(여기선 DB 커넥션이지만 테스트에선 독립 DB 사용)에 동시 접속을 시도해야 합니다.
        
        var (db1, _) = CreateScopedDatabase();
        var (db2, _) = CreateScopedDatabase();
        
        await SeedDataAsync(db1, "s1");
        await SeedDataAsync(db2, "s2");

        // 상태 관리는 싱글톤이므로 공유함
        var service1 = new TestableRouletteService(db1, _mockScopeFactory, _rouletteState, _mockLogger, _mockMediator, _mockOverlayState, _lockProvider);
        var service2 = new TestableRouletteService(db2, _mockScopeFactory, _rouletteState, _mockLogger, _mockMediator, _mockOverlayState, _lockProvider);

        service1.DelayExecuteSpinLogic = true;
        var taskS1 = Task.Run(() => service1.SpinRouletteAsync("s1", 1, "v1"));
        await Task.Delay(200);

        // s2는 다른 스트리머이므로 s1의 락과 상관없이 바로 통과해야 함
        var taskS2 = service2.SpinRouletteAsync("s2", 1, "v2");
        
        var resultS2 = await taskS2;
        resultS2.Should().NotBeNull();
        taskS1.IsCompleted.Should().BeFalse();

        service1.ReleaseDelay();
        await taskS1;
    }

    [Fact]
    public async Task SpinRoulette_Should_Handle_High_Load_1000_Requests()
    {
        var (db, _) = CreateScopedDatabase();
        var chzzkUid = "heavy_streamer";
        await SeedDataAsync(db, chzzkUid);

        var service = new TestableRouletteService(db, _mockScopeFactory, _rouletteState, _mockLogger, _mockMediator, _mockOverlayState, _lockProvider);
        var requestCount = 1000;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(i => service.SpinRouletteAsync(chzzkUid, 1, $"viewer_{i}"))
            .ToList();

        var startTime = DateTime.UtcNow;
        var results = await Task.WhenAll(tasks);
        var duration = DateTime.UtcNow - startTime;

        var successCount = results.Count(r => r != null);
        var timeoutCount = results.Count(r => r == null);

        _mockLogger.LogInformation("🎰 Stress Test: Success={SuccessCount}, Timeout={TimeoutCount}, Duration={Duration}s", 
            successCount, timeoutCount, duration.TotalSeconds);

        successCount.Should().BeGreaterThan(0);
        
        var logsInDb = await db.RouletteLogs.CountAsync();
        logsInDb.Should().Be(successCount);
    }

    private class TestableRouletteService : RouletteService
    {
        public bool DelayExecuteSpinLogic { get; set; }
        private readonly ManualResetEventSlim _mre = new(false);

        public TestableRouletteService(IAppDbContext db, IServiceScopeFactory scopeFactory, RouletteState rouletteState, ILogger<RouletteService> logger, IMediator mediator, OverlayState overlayState, IRouletteLockProvider lockProvider) 
            : base(db, scopeFactory, rouletteState, logger, mediator, overlayState, lockProvider)
        {
        }

        public void ReleaseDelay() => _mre.Set();

        protected override (List<RouletteItem> results, List<RouletteLog> logs) ExecuteSpinLogic(MooldangBot.Domain.Entities.Roulette roulette, GlobalViewer viewer, int count)
        {
            if (DelayExecuteSpinLogic)
            {
                _mre.Wait(); 
            }
            return base.ExecuteSpinLogic(roulette, viewer, count);
        }
    }
}
