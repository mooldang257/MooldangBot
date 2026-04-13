using System;
using System.Collections.Generic;

namespace MooldangBot.Domain.DTOs;

/// <summary>
/// [텔로스5의 순환]: 페이징된 데이터를 구조화하여 전달하는 record DTO입니다. (통합 명령어용)
/// </summary>
public record UnifiedPagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int CurrentPage, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
