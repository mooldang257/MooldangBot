namespace MooldangBot.Contracts.Requests.Point.Models;

/// <summary>
/// 포인트 적립/차감 트랜잭션 수집 작업 단위
/// </summary>
public record PointJob(string StreamerUid, string ViewerUid, string Nickname, int Amount);
