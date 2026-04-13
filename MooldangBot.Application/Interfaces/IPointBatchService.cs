using MooldangBot.Contracts.Requests.Point.Models;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [?ㅻ쾭?쒕씪?대툕 ?섏쭛湲?: ?ㅼ떆媛꾩쑝濡??잛븘吏???ъ씤???곷┰ ?붿껌???섏쭛?섎뒗 ?명꽣?섏씠?ㅼ엯?덈떎.
/// (N3/M3): 鍮꾩감??Non-blocking) ?먮? ?듯빐 梨꾪똿 泥섎━ ?띾룄???곹뼢??二쇱? ?딆뒿?덈떎.
/// </summary>
public interface IPointBatchService
{
    /// <summary>
    /// ?ъ씤?몃? ?곷┰ ?먯뿉 異붽??⑸땲??
    /// </summary>
    /// <param name="streamerUid">?ㅽ듃由щ㉧ ?앸퀎??/param>
    /// <param name="viewerUid">?쒖껌???앸퀎??/param>
    /// <param name="nickname">?쒖껌???됰꽕??/param>
    /// <param name="amount">?곷┰ 湲덉븸</param>
    void EnqueueIncrement(string streamerUid, string viewerUid, string nickname, int amount);

    /// <summary>
    /// ?꾩옱 ?먯뿉 ?볦씤 紐⑤뱺 ?묒뾽???뚯쭊(Drain)?섏뿬 諛섑솚?⑸땲??
    /// </summary>
    /// <param name="ct">痍⑥냼 ?좏겙</param>
    /// <returns>?곷┰ ?묒뾽 紐⑸줉</returns>
    IAsyncEnumerable<PointJob> DrainAllAsync(CancellationToken ct);

    /// <summary>
    /// ?섏쭛 以묐떒 諛?梨꾨꼸 醫낅즺 (Graceful Shutdown)
    /// </summary>
    void Complete();
}
