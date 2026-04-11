namespace MooldangBot.ChzzkAPI.Contracts.Models.Events;

/// <summary>
/// [?ㅼ떆由ъ뒪???뚰렪]: 移섏?吏?寃뚯씠?몄썾?댁뿉??諛쒖깮?섎뒗 梨꾪똿 ?대깽???곗씠??洹쒓꺽?낅땲??
/// ?몃? ?꾨찓???섏〈?깆쓣 諛곗젣?섍린 ?꾪빐 Contracts ?꾨줈?앺듃 ?댁뿉 ?뺤쓽?섏뿀?듬땲??
/// </summary>
public record ChzzkChatEvent
{
    public required string ChzzkUid { get; init; }
    public required string Message { get; init; }
    public string? Nickname { get; init; }
    public long Timestamp { get; init; }
}
