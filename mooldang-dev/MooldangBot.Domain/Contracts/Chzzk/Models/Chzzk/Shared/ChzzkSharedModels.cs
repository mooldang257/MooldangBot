using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Shared;

// [오시리스의 봉투]: 모든 치지직 API 응답의 공통 구조입니다.
public class ChzzkApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("content")]
    public T? Content { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Code == 200;
}

// [오시리스의 서식]: 페이지네이션이 포함된 리스트 응답 구조입니다.
public class ChzzkPagedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("page")]
    public ChzzkPagination? Page { get; set; }
}

// [오시리스의 지표]: 다양한 페이지 필드를 수용하는 통합 페이지 모델입니다.
public class ChzzkPagination
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}
