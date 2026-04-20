namespace MooldangBot.Domain.Common;

// 📥 페이징 요청 규격 (.NET 10 record 활용)
public record CursorPagedRequest(
    long? Cursor = 0, 
    int Limit = 20, 
    string? Search = null,
    string? Sort = null
);

// 📤 페이징 응답 규격 (커서 기반)
public record CursorPagedResponse<T>(
    IReadOnlyList<T> Items, 
    long? NextCursor,
    bool HasNext
);
