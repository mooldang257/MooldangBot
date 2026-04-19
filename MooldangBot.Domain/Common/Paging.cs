namespace MooldangBot.Domain.Common;

// 📥 페이징 요청 규격 (.NET 10 record 활용)
public record PagedRequest(
    int? LastId = 0, 
    int PageSize = 20, 
    string? Search = null
);

// 📤 페이징 응답 규격
public record PagedResponse<T>(
    List<T> Data, 
    long? NextLastId
);
