using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence.Converters;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

public class StreamerProfileConfiguration : IEntityTypeConfiguration<StreamerProfile>
{
    private readonly EncryptedValueConverter _converter;

    public StreamerProfileConfiguration(EncryptedValueConverter converter)
    {
        _converter = converter;
    }

    public void Configure(EntityTypeBuilder<StreamerProfile> builder)
    {
        builder.ToTable("core_streamer_profiles");

        // ⭐ 검색 성능 최적화를 위한 인덱스 추가
        builder.HasIndex(p => p.ChzzkUid).IsUnique();
        builder.HasIndex(p => p.Slug).IsUnique();

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

public class GlobalViewerConfiguration : IEntityTypeConfiguration<GlobalViewer>
{
    private readonly EncryptedValueConverter _converter;

    public GlobalViewerConfiguration(EncryptedValueConverter converter)
    {
        _converter = converter;
    }

    public void Configure(EntityTypeBuilder<GlobalViewer> builder)
    {
        builder.ToTable("core_global_viewers");

        var ciCollation = "utf8mb4_unicode_ci";

        // 데이터베이스 엔진별 ColumnType은 명시적으로 지정
        builder.Property(e => e.ViewerUid)
               .HasColumnType("longtext")
               .HasConversion<string?>(_converter);
               
        builder.Property(e => e.Nickname).UseCollation(ciCollation); // [v6.2] 중앙 닉네임
        
        builder.Property(e => e.ViewerUidHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.ViewerUidHash).IsUnique().HasDatabaseName("IX_GlobalViewer_ViewerUidHash");
        
        // 🚀 [v6.2.2] 닉네임 기반 시청자 검색 성능 최적화 (오시리스의 눈)
        builder.HasIndex(e => e.Nickname).HasDatabaseName("IX_GlobalViewer_Nickname");
    }
}

public class StreamerManagerConfiguration : IEntityTypeConfiguration<StreamerManager>
{
    public void Configure(EntityTypeBuilder<StreamerManager> builder)
    {
        builder.ToTable("core_streamer_managers");
        
        builder.HasOne(m => m.StreamerProfile)
               .WithMany()
               .HasForeignKey(m => m.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.GlobalViewer)
               .WithMany()
               .HasForeignKey(m => m.GlobalViewerId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChzzkCategoryConfiguration : IEntityTypeConfiguration<ChzzkCategory>
{
    public void Configure(EntityTypeBuilder<ChzzkCategory> builder)
    {
        builder.ToTable("sys_chzzk_categories");

        builder.HasMany(c => c.Aliases)
               .WithOne(a => a.Category)
               .HasForeignKey(a => a.CategoryId);
    }
}

public class ChzzkCategoryAliasConfiguration : IEntityTypeConfiguration<ChzzkCategoryAlias>
{
    public void Configure(EntityTypeBuilder<ChzzkCategoryAlias> builder)
    {
        builder.ToTable("sys_chzzk_category_aliases");
        
        builder.HasIndex(a => a.Alias);
    }
}

// [v4.9.4] 물댕봇 개인화 설정 (Permanent Preferences)
public class StreamerPreferenceConfiguration : IEntityTypeConfiguration<StreamerPreference>
{
    public void Configure(EntityTypeBuilder<StreamerPreference> builder)
    {
        builder.ToTable("sys_streamer_preferences");
        
        builder.HasOne(p => p.StreamerProfile)
               .WithMany()
               .HasForeignKey(p => p.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
        
        // [오시리스의 인덱싱]: 사용자별 설정 키는 유니크해야 하며, 조회 성능을 위해 인덱싱함
        builder.HasIndex(p => new { p.StreamerProfileId, p.PreferenceKey }).IsUnique();
    }
}

// 시청자와 스트리머 간의 관계 및 채널별 고유 상태
public class ViewerRelationConfiguration : IEntityTypeConfiguration<ViewerRelation>
{
    public void Configure(EntityTypeBuilder<ViewerRelation> builder)
    {
        builder.ToTable("viewer_relations");
        
        builder.HasOne(v => v.StreamerProfile)
               .WithMany()
               .HasForeignKey(v => v.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(v => v.GlobalViewer)
               .WithMany()
               .HasForeignKey(v => v.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        // [v18.2] UPSERT 무결성을 위한 복합 유니크 인덱스 추가
        builder.HasIndex(v => new { v.StreamerProfileId, v.GlobalViewerId }).IsUnique();
    }
}
