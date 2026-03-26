namespace MooldangAPI.Features.Roulette;

public class RouletteState
{
    private bool _isSpinning;
    private readonly object _lock = new();

    public bool TryStartSpin()
    {
        lock (_lock)
        {
            if (_isSpinning) return false;
            _isSpinning = true;
            return true;
        }
    }

    public void StopSpin()
    {
        lock (_lock)
        {
            _isSpinning = false;
        }
    }

    public bool IsSpinning => _isSpinning;
}
