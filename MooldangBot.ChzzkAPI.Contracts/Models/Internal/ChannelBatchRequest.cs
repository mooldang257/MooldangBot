using System.Collections.Generic;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Internal;

/// <summary>
/// [오시리스의 집계]: 채널 ID 목록을 받아 배치를 요청하기 위한 모델입니다.
/// </summary>
public record ChannelBatchRequest(List<string> ChannelIds);
