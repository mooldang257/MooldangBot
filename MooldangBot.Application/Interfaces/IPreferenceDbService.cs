namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 지혜]: MariaDB를 활용한 스트리머별 영구 설정 관리 인터페이스입니다.
/// </summary>
public interface IPreferenceDbService
{
    /// <summary>
    /// 영구 설정을 저장하거나 업데이트합니다.
    /// </summary>
    Task SetPermanentPreferenceAsync(string chzzkUid, string key, string value);

    /// <summary>
    /// 특정 영구 설정을 조회합니다.
    /// </summary>
    Task<string?> GetPermanentPreferenceAsync(string chzzkUid, string key);

    /// <summary>
    /// 특정 영구 설정을 삭제합니다.
    /// </summary>
    Task RemovePermanentPreferenceAsync(string chzzkUid, string key);
}
