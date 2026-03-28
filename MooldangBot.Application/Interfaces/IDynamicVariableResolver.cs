using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces
{
    /// <summary>
    /// [v4.4.0] 챗봇의 동적 변수를 내부 메서드 호출로 해석하는(Resolve) 인터페이스
    /// </summary>
    public interface IDynamicVariableResolver
    {
        Task<string?> ResolveAsync(string methodName, string streamerUid, string viewerUid);
    }
}
