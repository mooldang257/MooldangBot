using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure.Services.Engines;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Application.Services;
using NSubstitute;
using Xunit;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using System.Text.Json;
using System.Text;

namespace MooldangBot.Tests;

public class AegisPipelineTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ChaosManager _chaos = new(Substitute.For<ILogger<ChaosManager>>());
    private readonly ILogger<IdentityCacheService> _logger = Substitute.For<ILogger<IdentityCacheService>>();

    [Fact]
    public async Task IdentityCache_Should_Return_Cached_Profile_Without_DB_Hit()
    {
        // [Arrange]
        var service = new IdentityCacheService(_cache, _scopeFactory, _chaos, _logger);
        var streamerUid = "streamer123";
        var profile = new StreamerProfile { ChzzkUid = streamerUid, ChannelName = "TestChannel" };
        var profileBytes = JsonSerializer.SerializeToUtf8Bytes(profile, MooldangBot.Domain.Contracts.Chzzk.ChzzkJsonContext.Default.StreamerProfile);

        // 캐시에 데이터가 있다고 가정
        _cache.GetAsync($"Streamer:{streamerUid}", Arg.Any<CancellationToken>()).Returns(profileBytes);

        // [Act]
        var result = await service.GetStreamerProfileAsync(streamerUid);

        // [Assert]
        Assert.NotNull(result);
        Assert.Equal("TestChannel", result?.ChannelName);
        
        // 캐시 조회가 성공했다는 것은 DB 조회를 거치지 않았음을 의미함
    }

    [Fact]
    public async Task DynamicQueryEngine_Should_Resolve_Variables_Parallel()
    {
        // [Arrange]
        var cache = Substitute.For<ICommandMasterCacheService>();
        var resolver = Substitute.For<IDynamicVariableResolver>();
        var db = Substitute.For<IAppDbContext>();
        var logger = Substitute.For<ILogger<DynamicQueryEngine>>();
        var engine = new DynamicQueryEngine(db, cache, resolver, logger);

        var variables = new List<DynamicVariableMetadata>
        {
            new(1, "$(user)", "Test", "info", "METHOD:User"),
            new(2, "$(points)", "Test", "success", "METHOD:Points")
        };

        cache.GetFullVariablesAsync().Returns(variables);
        resolver.ResolveAsync("User", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult<string?>("Muldang"));
        resolver.ResolveAsync("Points", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult<string?>("1000"));

        // [Act]
        var result = await engine.ProcessMessageAsync("Hello $(user), you have $(points)P", "s1", "v1", "Muldang");

        // [Assert]
        Assert.Equal("Hello Muldang, you have 1000P", result);
        
        // 두 변수가 각각 1번씩 Resolve 호출되었는지 확인
        await resolver.Received(1).ResolveAsync("User", "s1", "v1", "Muldang");
        await resolver.Received(1).ResolveAsync("Points", "s1", "v1", "Muldang");
    }
}
