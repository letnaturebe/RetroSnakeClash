namespace SnakeServer;

public class Heartbeat
{
    public const int MAX_HEARTBEAT_SEC = 2;
    private readonly Timer _timer;
    private readonly float _interval;
    private readonly Action _onTimer;
    
    public Heartbeat(float interval, Action onTimer)
    {
        
        _interval = interval;
        _onTimer = onTimer;
        _timer = new Timer(OnTimer, null, 0, MAX_HEARTBEAT_SEC * 1000);
    }
    
    private void OnTimer(object? state)
    {
        _onTimer();
    }
    public void Disable()
    {
        _timer.Dispose();
    }
}