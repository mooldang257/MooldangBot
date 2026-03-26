using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface IObsWebSocketService
{
    Task ConnectAsync(string chzzkUid);
    Task DisconnectAsync(string chzzkUid);
    Task ChangeSceneAsync(string chzzkUid, string sceneName);
}
