using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence.Converters;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class StreamerProfileConfiguration : IEntityTypeConfiguration<CoreStreamerProfiles>
{
    private readonly EncryptedValueConverter _converter;

    public StreamerProfileConfiguration(EncryptedValueConverter converter)
    {
        _converter = converter;
    }

    public void Configure(EntityTypeBuilder<CoreStreamerProfiles> builder)
    {

        // ⭐ 검색 성능 최적화를 위한 인덱스 추가
        builder.HasIndex(p => p.ChzzkUid).IsUnique();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.OverlayToken); // [v2.4.1] 오버레이 토큰 기반 검색 성능 최적화

        // 🔍 대소문자 무관 검색을 위한 명시적 Collation 설정 (Osiris)
        var ciCollation = "utf8mb4_unicode_ci";
        
        // EF Core 7.0+ 기준 Database Provider 체크는 Configuration 밖에서 하거나, 
        // MySql에 종속적인 속성이면 직접 부여합니다.
        builder.Property(e => e.ChzzkUid).UseCollation(ciCollation);
        builder.Property(e => e.Slug).UseCollation(ciCollation);

        // [v4.0] 수호자의 암호: 암호화 필드 설정 및 길이 확장 (Nullable 지원)
        builder.Property(e => e.ChzzkAccessToken).HasConversion(_converter);
        builder.Property(e => e.ChzzkRefreshToken).HasConversion(_converter);
    }
}

public class GlobalViewerConfiguration : IEntityTypeConfiguration<CoreGlobalViewers>
{
    private readonly EncryptedValueConverter _converter;

    public GlobalViewerConfiguration(EncryptedValueConverter converter)
    {
        _converter = converter;
    }

    public void Configure(EntityTypeBuilder<CoreGlobalViewers> builder)
    {

        var ciCollation = "utf8mb4_unicode_ci";

        // 데이터베이스 엔진별 ColumnType은 명시적으로 지정
        builder.Property(e => e.ViewerUid)
               .HasMaxLength(500)
               .HasConversion<string?>(_converter);

        builder.HasIndex(e => e.ViewerUid).IsUnique().HasDatabaseName("IX_GlobalViewer_ViewerUid");
               
        builder.Property(e => e.Nickname).UseCollation(ciCollation); // [v6.2] 중앙 닉네임
        
        builder.Property(e => e.ViewerUidHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.ViewerUidHash).IsUnique().HasDatabaseName("IX_GlobalViewer_ViewerUidHash");
        
        // 🚀 [v6.2.2] 닉네임 기반 시청자 검색 성능 최적화 (오시리스의 눈)
        builder.HasIndex(e => e.Nickname).HasDatabaseName("IX_GlobalViewer_Nickname");
    }
}

public class StreamerManagerConfiguration : IEntityTypeConfiguration<CoreStreamerManagers>
{
    public void Configure(EntityTypeBuilder<CoreStreamerManagers> builder)
    {
        
        builder.HasOne(m => m.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(m => m.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(m => m.GlobalViewerId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChzzkCategoryConfiguration : IEntityTypeConfiguration<SysChzzkCategories>
{
    public void Configure(EntityTypeBuilder<SysChzzkCategories> builder)
    {

        builder.HasMany(c => c.Aliases)
               .WithOne(a => a.Category)
               .HasForeignKey(a => a.CategoryId);
    }
}

public class ChzzkCategoryAliasConfiguration : IEntityTypeConfiguration<SysChzzkCategoryAliases>
{
    public void Configure(EntityTypeBuilder<SysChzzkCategoryAliases> builder)
    {
        
        builder.HasIndex(a => a.Alias);
    }
}

// [v4.9.4] 물댕봇 개인화 설정 (Permanent Preferences)
public class StreamerPreferenceConfiguration : IEntityTypeConfiguration<SysStreamerPreferences>
{
    public void Configure(EntityTypeBuilder<SysStreamerPreferences> builder)
    {
        
        builder.HasOne(p => p.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
        
        // [오시리스의 인덱싱]: 사용자별 설정 키는 유니크해야 하며, 조회 성능을 위해 인덱싱함
        builder.HasIndex(p => new { p.StreamerProfileId, p.PreferenceKey }).IsUnique();
    }
}

// 시청자와 스트리머 간의 관계 및 채널별 고유 상태
public class ViewerRelationConfiguration : IEntityTypeConfiguration<CoreViewerRelations>
{
    public void Configure(EntityTypeBuilder<CoreViewerRelations> builder)
    {
        
        builder.HasOne(v => v.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(v => v.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(v => v.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        // [v18.2] UPSERT 무결성을 위한 복합 유니크 인덱스 추가
        builder.HasIndex(v => new { v.StreamerProfileId, v.GlobalViewerId }).IsUnique();
    }
}
