namespace TimerApp.Core.Models;

public enum TimerState { Idle, Running, Paused, Completed }

public class TimerInstance
{
    public TimerConfig Config { get; }
    public TimerState State { get; private set; } = TimerState.Idle;
    public TimeSpan Remaining { get; private set; }
    public bool IsLooping { get; set; }

    public event EventHandler<TimerInstance>? Elapsed;
    public event EventHandler<TimerInstance>? Ticked;

    public TimerInstance(TimerConfig config)
    {
        Config    = config;
        Remaining = config.Duration;
        IsLooping = config.IsLooping;
    }

    public void Start()
    {
        if (State == TimerState.Idle || State == TimerState.Paused)
            State = TimerState.Running;
    }

    public void Pause()
    {
        if (State == TimerState.Running)
            State = TimerState.Paused;
    }

    public void Reset()
    {
        State     = TimerState.Idle;
        Remaining = Config.Duration;
    }

    internal void Advance(TimeSpan elapsed)
    {
        if (State != TimerState.Running) return;

        Remaining -= elapsed;
        OnTicked();

        if (Remaining <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            State     = TimerState.Completed;
            OnElapsed();

            if (IsLooping)
            {
                Remaining = Config.Duration;
                State     = TimerState.Running;
            }
        }
    }

    protected virtual void OnElapsed() => Elapsed?.Invoke(this, this);
    protected virtual void OnTicked()  => Ticked?.Invoke(this, this);
}
