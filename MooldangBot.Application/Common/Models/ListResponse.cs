using System.Collections.Generic;

namespace MooldangBot.Application.Common.Models;

/// <summary>
/// [v10.1] 오시리스 전역 리스트 표준 응답 규격
/// 모든 배열 데이터는 향후 페이지네이션과 메타데이터 확장을 위해 이 규격으로 래핑됩니다.
/// </summary>
/// <typeparam name="T">리스트 아이템 타입</typeparam>
/// <param name="Items">실제 데이터 목록</param>
/// <param name="TotalCount">전체 아이템 수 (페이지네이션용)</param>
public record ListResponse<T>(IReadOnlyList<T> Items, int TotalCount);
