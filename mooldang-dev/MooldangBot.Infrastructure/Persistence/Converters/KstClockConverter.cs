using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MooldangBot.Domain.Common;
using System;

namespace MooldangBot.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core용 KstClock 밸류 컨버터입니다. (v2.0)
/// DB의 Unspecified Kind를 KstClock으로 안전하게 변환합니다.
/// </summary>
public class KstClockConverter : ValueConverter<KstClock, DateTime>
{
    public KstClockConverter() : base(
        v => v.Value, // To DB (KstClock -> DateTime)
        v => KstClock.FromDateTime(DateTime.SpecifyKind(v, DateTimeKind.Unspecified)) // From DB
    ) { }
}
