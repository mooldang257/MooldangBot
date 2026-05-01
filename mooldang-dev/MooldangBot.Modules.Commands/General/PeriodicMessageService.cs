using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Commands.General;

public class PeriodicMessageService(IAppDbContext db) : IPeriodicMessageService
{
    public async Task<CursorPagedResponse<PeriodicMessageDto>> GetListAsync(string chzzkUid, CursorPagedRequest request)
    {
        return await db.SysPeriodicMessages
            .Include(m => m.StreamerProfile)
            .Where(m => m.StreamerProfile!.ChzzkUid == chzzkUid)
            .OrderByDescending(m => m.Id)
            .Select(m => new PeriodicMessageDto
            {
                Id = m.Id,
                IntervalMinutes = m.IntervalMinutes,
                Message = m.Message,
                IsEnabled = m.IsEnabled
            })
            .ToPagedListAsync(request.Limit, m => m.Id);
    }

    public async Task<Result<bool>> SaveAsync(string chzzkUid, PeriodicMessageSaveRequest req)
    {
        if (req.Id > 0)
        {
            var existing = await db.SysPeriodicMessages
                .IgnoreQueryFilters()
                .Include(m => m.StreamerProfile)
                .FirstOrDefaultAsync(m => m.Id == req.Id && m.StreamerProfile!.ChzzkUid == chzzkUid);
                
            if (existing == null)
                return Result<bool>.Failure("해당 메시지를 찾을 수 없습니다.");

            existing.IntervalMinutes = req.IntervalMinutes;
            existing.Message = req.Message;
            existing.IsEnabled = req.IsEnabled;
        }
        else
        {
            var profile = await db.CoreStreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
            if (profile == null) 
                return Result<bool>.Failure("스트리머 프로필을 찾을 수 없습니다.");

            db.SysPeriodicMessages.Add(new PeriodicMessage
            {
                StreamerProfileId = profile.Id,
                IntervalMinutes = req.IntervalMinutes,
                Message = req.Message,
                IsEnabled = req.IsEnabled
            });
        }

        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(string chzzkUid, int id)
    {
        var item = await db.SysPeriodicMessages
            .IgnoreQueryFilters()
            .Include(m => m.StreamerProfile)
            .FirstOrDefaultAsync(m => m.Id == id && m.StreamerProfile!.ChzzkUid == chzzkUid);
            
        if (item == null)
            return Result<bool>.Failure("해당 메시지를 찾을 수 없습니다.");

        db.SysPeriodicMessages.Remove(item);
        await db.SaveChangesAsync();
        
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ToggleAsync(string chzzkUid, int id)
    {
        var item = await db.SysPeriodicMessages
            .IgnoreQueryFilters()
            .Include(m => m.StreamerProfile)
            .FirstOrDefaultAsync(m => m.Id == id && m.StreamerProfile!.ChzzkUid == chzzkUid);
            
        if (item == null)
            return Result<bool>.Failure("해당 메시지를 찾을 수 없습니다.");

        item.IsEnabled = !item.IsEnabled;
        await db.SaveChangesAsync();
        
        return Result<bool>.Success(true);
    }
}
