using System.Diagnostics;
using System.Windows.Media;

namespace AsteroidsGC.Engine;

public sealed class GameLoop
{
    public event Action<float>? Tick;

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _stopwatch.Restart();
        _lastElapsed = _stopwatch.Elapsed;
        
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;
        float dt = (float)(elapsed - _lastElapsed).TotalSeconds;
        _lastElapsed = elapsed;

        if (dt < 0)
        {
            dt = 0;
        }
        else if (dt > MaxDeltaSeconds)
        {
            dt = MaxDeltaSeconds;
        }

        Tick?.Invoke(dt);
    }

    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastElapsed;
    private bool _running;

    private const float MaxDeltaSeconds = 0.1f;
}
