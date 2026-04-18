using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MooldangBot.Contracts.Chzzk.Models.Chzzk.Shared;

// [오시리스??遊됲닾]: 紐⑤뱺 치지직API ?묐떟??怨듯넻 援ъ“?낅땲??
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

// [오시리스???묒떇]: ?섏씠吏뺤씠 ?ы븿??由ъ뒪???묐떟 援ъ“?낅땲??
public class ChzzkPagedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("page")]
    public ChzzkPagination? Page { get; set; }
}

// [오시리스??吏??: ?ㅼ뼇???섏씠吏??꾨뱶瑜??섏슜?섎뒗 ?듯빀 ?섏씠吏?紐⑤뜽?낅땲??
public class ChzzkPagination
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}
