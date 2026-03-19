namespace MooldangAPI.Strategies;

public interface IOverlayRenderStrategy
{
    string RenderChatHtml(string username, string message);
}

public class DefaultChatRenderStrategy : IOverlayRenderStrategy
{
    public string RenderChatHtml(string username, string message)
    {
        // XSS 방지 등을 위해 보통은 프론트엔드에서 HTML 생성을 하지만, 
        // 요구사항에 따른 Strategy 패턴 예시를 위해 백엔드 렌더링을 구현합니다.
        return $"<div class=\"chat-msg default\"><strong>{username}</strong>: {message}</div>";
    }
}
