using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MooldangBot.Domain.Entities;

/// <summary>
/// [세피로스의 각인]: 명령어 액션 타입을 강타입 Enum으로 관리합니다.
/// </summary>
public enum CommandFeatureType
{
    Unknown = 0,
    Reply = 1,           // 단순 채팅 답장
    Notice = 2,          // 공지
    Title = 3,           // 방제
    Category = 4,        // 카테고리
    SonglistToggle = 5,  // 송리스트 토글
    SongRequest = 6,     // 노래 신청
    Omakase = 7,         // 오마카세
    Roulette = 8,        // 룰렛
    ChatPoint = 9,       // 채팅 포인트 (적립 등)
    SystemResponse = 10, // 시스템 응답
    AI = 11,             // AI 답변
    Attendance = 12,     // 출석체크
    PointCheck = 13      // 포인트 확인
}

/// <summary>
/// [마스터 데이터]: 명령어 기능(Feature) 상세 정의 메타데이터
/// </summary>
public record CommandFeatureMetadata(
    CommandFeatureType Type,
    int CategoryId,
    string TypeName,
    string DisplayName,
    int DefaultCost,
    CommandRole RequiredRole,
    bool IsEnabled = true
);

/// <summary>
/// [세피로스의 보관소]: 명령어 기능들의 정의를 코드 레벨에서 관리하는 레지스트리입니다.
/// </summary>
public static class CommandFeatureRegistry
{
    private static readonly ImmutableList<CommandFeatureMetadata> _features = ImmutableList.Create(
        new CommandFeatureMetadata(CommandFeatureType.Reply, 1, "Reply", "텍스트 답변", 0, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.Notice, 2, "Notice", "공지", 0, CommandRole.Manager),
        new CommandFeatureMetadata(CommandFeatureType.Title, 2, "Title", "방제", 0, CommandRole.Manager),
        new CommandFeatureMetadata(CommandFeatureType.Category, 2, "Category", "카테고리", 0, CommandRole.Manager),
        new CommandFeatureMetadata(CommandFeatureType.SonglistToggle, 2, "SonglistToggle", "송리스트", 0, CommandRole.Manager),
        new CommandFeatureMetadata(CommandFeatureType.SongRequest, 3, "SongRequest", "노래신청", 1000, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.Omakase, 3, "Omakase", "오마카세", 1000, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.Roulette, 3, "Roulette", "룰렛", 500, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.ChatPoint, 3, "ChatPoint", "채팅포인트", 0, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.SystemResponse, 2, "SystemResponse", "시스템 응답", 0, CommandRole.Manager),
        new CommandFeatureMetadata(CommandFeatureType.AI, 3, "AI", "AI 답변", 1000, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.Attendance, 3, "Attendance", "출석체크", 10, CommandRole.Viewer),
        new CommandFeatureMetadata(CommandFeatureType.PointCheck, 1, "PointCheck", "포인트확인", 0, CommandRole.Viewer)
    );

    public static IReadOnlyList<CommandFeatureMetadata> All => _features;

    public static CommandFeatureMetadata? GetByType(CommandFeatureType type) 
        => _features.FirstOrDefault(f => f.Type == type);

    public static CommandFeatureMetadata? GetByTypeName(string typeName) 
        => _features.FirstOrDefault(f => string.Equals(f.TypeName, typeName, StringComparison.OrdinalIgnoreCase));

    public static CommandFeatureType FromId(int id) => (CommandFeatureType)id;
}
