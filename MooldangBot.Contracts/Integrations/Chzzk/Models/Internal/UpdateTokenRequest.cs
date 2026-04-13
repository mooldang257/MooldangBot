namespace MooldangBot.Contracts.Integrations.Chzzk.Models.Internal;

/// <summary>
/// [오시리스??蹂댁븞 ?듭떊]: 硫붿씤 ?붿쭊?먯꽌 寃뚯씠?몄썾?대줈 전송?섎뒗 ?좏겙 媛깆떊 ?붿껌 紐⑤뜽?낅땲??
/// </summary>
public record UpdateTokenRequest(
    string ChzzkUid, 
    string SessionCookie, 
    string AuthCookie);
