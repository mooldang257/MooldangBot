using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;

namespace MooldangAPI.Services
{
    public class ObsWebSocketService
    {
        private readonly OBSWebsocket _obs;
        private readonly ILogger<ObsWebSocketService> _logger;
        private bool _isConnected = false;

        public ObsWebSocketService(ILogger<ObsWebSocketService> logger)
        {
            _obs = new OBSWebsocket();
            _logger = logger;

            _obs.Connected += (s, e) => {
                _isConnected = true;
                _logger.LogInformation("OBS WebSocket Connected Event");
            };
            _obs.Disconnected += (s, e) => {
                _isConnected = false;
                _logger.LogWarning("OBS WebSocket Disconnected");
            };
        }

        public async Task ConnectAsync(string url, string password)
        {
            try
            {
                if (_isConnected) return;
                
                // OBS WebSocket 연결 시도 (포트 4455가 기본)
                _obs.ConnectAsync(url, password);
                _isConnected = true; 
                _logger.LogInformation("Connected to OBS WebSocket");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to OBS WebSocket: {ex.Message}");
                _isConnected = false;
            }
        }

        public void SetScene(string sceneName)
        {
            if (!_isConnected) return;

            try
            {
                _obs.SetCurrentProgramScene(sceneName);
                _logger.LogInformation($"OBS Scene changed to: {sceneName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set OBS scene: {ex.Message}");
            }
        }

    }
}
