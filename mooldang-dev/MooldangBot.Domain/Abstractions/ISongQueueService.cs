using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Domain.Abstractions;

public interface ISongQueueService
{
    Task<CursorPagedResponse<SongQueueViewDto>> GetPagedQueueAsync(string chzzkUid, SongStatus? status, CursorPagedRequest request);
    Task<Result<SongQueueResponseDto>> AddSongAsync(string chzzkUid, SongAddRequest request, int? omakaseId = null);
    Task<Result<bool>> UpdateStatusAsync(string chzzkUid, int id, SongStatus status);
    Task<Result<bool>> DeleteSongsAsync(string chzzkUid, List<int> ids);
    Task<Result<SongQueueResponseDto>> UpdateSongDetailsAsync(string chzzkUid, int id, SongUpdateRequest request);
    Task<Result<int>> ClearSongsByStatusAsync(string chzzkUid, SongStatus status);
    Task<Result<bool>> ReorderSongsAsync(string chzzkUid, List<int> ids);
}
