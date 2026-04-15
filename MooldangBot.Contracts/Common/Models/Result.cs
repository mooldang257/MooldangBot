namespace MooldangBot.Contracts.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string? Error { get; set; }
    public object? Errors { get; set; } // 異붽?: ?곸꽭 ?먮윭 ?뺣낫 (v6.2.2)
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> Failure(string error, object? errors) => new() { IsSuccess = false, Error = error, Errors = errors };
}
