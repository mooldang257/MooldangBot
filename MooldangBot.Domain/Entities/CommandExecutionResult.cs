namespace MooldangBot.Domain.Entities;

/// <summary>
/// [세피로스의 증명]: 명령어 실행 결과를 담는 레코드입니다.
/// </summary>
/// <param name="IsSuccess">성공 여부</param>
/// <param name="Message">사용자에게 전달할 메시지 (실패 시 에러 메시지)</param>
/// <param name="ErrorCode">오류 코드 (필요 시)</param>
/// <param name="ShouldRefund">재화 환불이 필요한지 여부 (보상 트랜잭션 트리거)</param>
public record CommandExecutionResult(
    bool IsSuccess, 
    string Message = "", 
    string? ErrorCode = null, 
    bool ShouldRefund = false
)
{
    public static CommandExecutionResult Success(string message = "") => new(true, message);
    public static CommandExecutionResult Failure(string message, bool shouldRefund = true, string? errorCode = null) 
        => new(false, message, errorCode, shouldRefund);
}
