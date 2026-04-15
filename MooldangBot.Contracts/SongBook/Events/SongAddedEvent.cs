using MediatR;

namespace MooldangBot.Contracts.SongBook.Events;

/// <summary>
/// [?ㅼ떆由ъ뒪??怨듬챸]: ?덈줈???몃옒 ?좎껌???깃났?곸쑝濡??묒닔?섏뿀?뚯쓣 ?뚮━???듯빀 ?대깽?몄엯?덈떎.
/// </summary>
public record SongAddedEvent(string Username, string SongTitle, string? ChzzkUid = null) : INotification;
