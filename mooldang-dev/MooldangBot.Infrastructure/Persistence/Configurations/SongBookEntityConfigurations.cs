using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Infrastructure.Persistence.Configurations;

// [v19.0] 고도화된 송북 매핑: '완성형' 구조 반영
public class SongBookConfiguration : IEntityTypeConfiguration<FuncSongBooks>
{
    public void Configure(EntityTypeBuilder<FuncSongBooks> builder)
    {
        
        builder.HasIndex(s => new { s.StreamerProfileId, s.SongNo }).IsUnique();
        builder.HasIndex(s => s.Title);
        builder.HasIndex(s => s.TitleChosung);
        builder.HasIndex(s => s.Alias);
        builder.HasIndex(s => s.Category);
        builder.HasIndex(s => s.IsRequestable);
        builder.HasIndex(s => s.SongLibraryId);
    }
}

public class SongQueueConfiguration : IEntityTypeConfiguration<FuncSongListQueues>
{
    public void Configure(EntityTypeBuilder<FuncSongListQueues> builder)
    {

        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.CoreGlobalViewers)
               .WithMany()
               .HasForeignKey(s => s.GlobalViewerId)
               .OnDelete(DeleteBehavior.Restrict);

        // [v6.2.2] 노래책 연동 (선택 사항)
        builder.HasOne(s => s.FuncSongBooks)
               .WithMany()
               .HasForeignKey(s => s.SongBookId)
               .OnDelete(DeleteBehavior.SetNull); // 노래책 항목이 삭제되어도 신청 기록은 유지
 
        builder.HasIndex(e => e.StreamerProfileId);
        
        // [v13.1] Snowflake ID 검색 최적화
        builder.HasIndex(e => e.SongLibraryId); 

        // [v6.2.2] 상태값 Enum 변환
        builder.Property(e => e.Status).HasConversion<int>();
        
        // 🚀 [Phase 2] 커서 기반 페이지네이션 최적화
        builder.HasIndex(e => new { e.StreamerProfileId, e.Status, e.Id })
               .IsDescending(false, false, true)
               .HasDatabaseName("IX_SongQueue_Status_Cursor");
    }
}

public class SonglistSessionConfiguration : IEntityTypeConfiguration<FuncSongListSessions>
{
    public void Configure(EntityTypeBuilder<FuncSongListSessions> builder)
    {

        builder.HasOne(s => s.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(s => s.StreamerProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.StreamerProfileId, s.IsActive });
    }
}

public class StreamerOmakaseItemConfiguration : IEntityTypeConfiguration<FuncSongListOmakases>
{
    public void Configure(EntityTypeBuilder<FuncSongListOmakases> builder)
    {
        
        builder.HasOne(o => o.CoreStreamerProfiles)
               .WithMany()
               .HasForeignKey(o => o.StreamerProfileId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// 🎵 [v12.0] 중앙 병기창 (Media Library) 매핑
public class MasterSongLibraryConfiguration : IEntityTypeConfiguration<FuncSongMasterLibrary>
{
    public void Configure(EntityTypeBuilder<FuncSongMasterLibrary> builder)
    {
        
        builder.HasIndex(e => e.SongLibraryId).IsUnique(); 
        builder.HasIndex(e => e.YoutubeUrl);
        builder.HasIndex(e => e.Title);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Album);
        builder.HasIndex(e => e.Alias);
        builder.HasIndex(e => e.TitleChosung);
        builder.HasIndex(e => e.ArtistChosung);
    }
}

// [v12.5] 스트리머 라이브러리
public class StreamerSongLibraryConfiguration : IEntityTypeConfiguration<FuncSongStreamerLibrary>
{
    public void Configure(EntityTypeBuilder<FuncSongStreamerLibrary> builder)
    {
        
        builder.HasIndex(e => e.SongLibraryId).IsUnique();
        builder.HasIndex(e => new { e.StreamerProfileId, e.SongLibraryId }).IsUnique();
    }
}

public class MasterSongStagingConfiguration : IEntityTypeConfiguration<FuncSongMasterStaging>
{
    public void Configure(EntityTypeBuilder<FuncSongMasterStaging> builder)
    {
        
        builder.HasIndex(e => e.SongLibraryId).IsUnique(); 
        builder.HasIndex(e => e.CreatedAt); // [v13.1] 백그라운드 삭제 성능 향상
        builder.HasIndex(e => e.YoutubeUrl);
        builder.HasIndex(e => e.TitleChosung);
        builder.HasIndex(e => e.ArtistChosung);
        
        builder.Property(e => e.SourceType).HasConversion<int>();
    }
}