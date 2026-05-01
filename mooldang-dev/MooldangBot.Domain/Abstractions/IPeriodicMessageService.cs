using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Abstractions;

public interface IPeriodicMessageService
{
    Task<CursorPagedResponse<PeriodicMessageDto>> GetListAsync(string chzzkUid, CursorPagedRequest request);
    Task<Result<bool>> SaveAsync(string chzzkUid, PeriodicMessageSaveRequest req);
    Task<Result<bool>> DeleteAsync(string chzzkUid, int id);
    Task<Result<bool>> ToggleAsync(string chzzkUid, int id);
}

public class PeriodicMessageSaveRequest
{
    public int Id { get; set; }
    public int IntervalMinutes { get; set; }
    public string Message { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
