namespace Pace;

// One eased transition, stepped on the owner's display frames so motion stays in sync
// with rendering. Callers cancel on detachment or window close.
internal sealed class MotionTween(Visual owner) : IDisposable
{
    Action<double>? frame;
    Action? completed;
    Func<double, double> easing = Motion.EaseOut;
    long started;
    double duration;
    int generation;
    public bool IsRunning { get; private set; }

    public void Start(double milliseconds, Action<double> update, Action? done = null, Func<double, double>? ease = null)
    {
        Dispose();
        frame = update; completed = done; duration = milliseconds;
        easing = ease ?? Motion.EaseOut;
        started = Environment.TickCount64;
        if (!Motion.Enabled || Host() is not { } host)
        {
            Finish(1);
            return;
        }
        update(easing(0));
        IsRunning = true;
        int current = ++generation;
        host.RequestAnimationFrame(_ => Step(current));
    }

    void Step(int current)
    {
        if (!IsRunning || current != generation)
            return;
        double progress = Math.Clamp((Environment.TickCount64 - started) / duration, 0, 1);
        if (progress >= 1 || Host() is not { } host)
        {
            Finish(progress);
            return;
        }
        frame?.Invoke(easing(progress));
        host.RequestAnimationFrame(_ => Step(current));
    }

    void Finish(double progress)
    {
        IsRunning = false;
        frame?.Invoke(easing(Math.Max(progress, 1)));
        var done = completed;
        frame = null; completed = null;
        done?.Invoke();
    }

    // A hidden window draws no frames; finish immediately rather than stall.
    TopLevel? Host() => TopLevel.GetTopLevel(owner) is { } top && (top is not WindowBase window || window.IsVisible) ? top : null;

    public void Dispose()
    {
        IsRunning = false;
        generation++;
        frame = null; completed = null;
    }
}
