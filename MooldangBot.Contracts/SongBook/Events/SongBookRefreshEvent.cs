using MediatR;

namespace MooldangBot.Contracts.SongBook.Events;

/// <summary>
/// [?ㅼ떆由ъ뒪???뚮룞]: ?〓턿 ?뱀? ?ㅻ쭏移댁꽭 ?곹깭媛 蹂寃쎈릺???ㅻ쾭?덉씠 ?덈줈怨좎묠???꾩슂?⑥쓣 ?뚮┰?덈떎.
/// </summary>
public record SongBookRefreshEvent(string ChzzkUid) : INotification;
