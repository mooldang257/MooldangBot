using System.Collections.Generic;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [텔로스5의 선택]: 커서 기반 페이징 요청을 위한 record DTO입니다.
/// </summary>
/// <param name="Cursor">마지막으로 받은 데이터의 고유 ID (첫 요청 시 null)</param>
/// <param name="Limit">한 페이지에 표시할 데이터 개수 (기본 20)</param>
public record CursorPagedRequest(int? Cursor = null, int Limit = 20);

/// <summary>
/// [텔로스5의 순환]: 커서 기반 페이징 응답을 위한 공통 record DTO입니다.
/// </summary>
/// <typeparam name="T">데이터 항목의 타입</typeparam>
/// <param name="Items">필터링 및 페이징된 데이터 목록</param>
/// <param name="NextCursor">다음 페이지 탐색을 위한 커서 (마지막 페이지인 경우 null)</param>
/// <param name="HasNext">다음 페이지 존재 여부</param>
public record CursorPagedResponse<T>(
    IEnumerable<T> Items,
    int? NextCursor,
    bool HasNext
);
