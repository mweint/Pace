namespace Pace;

// One clock for all custom transitions; callers cancel on detachment or window close.
internal sealed class MotionTween : IDisposable
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(Motion.FrameMilliseconds) };
    Action<double>? frame;
    Action? completed;
    long started;
    double duration;
    Func<double, double> easing = Motion.EaseOut;
    public bool IsRunning => timer.IsEnabled;
    public MotionTween()
    {
        timer.Tick += (_, _) =>
        {
            double progress = Math.Clamp((Environment.TickCount64 - started) / duration, 0, 1);
            frame?.Invoke(easing(progress));
            if (progress < 1) return;
            timer.Stop();
            var done = completed;
            completed = null;
            done?.Invoke();
        };
    }
    public void Start(double milliseconds, Action<double> update, Action? done = null, Func<double, double>? ease = null)
    {
        timer.Stop();
        frame = update; completed = done; duration = milliseconds;
        easing = ease ?? Motion.EaseOut;
        started = Environment.TickCount64;
        if (!Motion.Enabled) { update(1); completed = null; done?.Invoke(); }
        else { update(0); timer.Start(); }
    }
    public void Dispose() { timer.Stop(); frame = null; completed = null; }
}
