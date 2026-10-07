namespace Usage;

public sealed class AnimatedAccountList : FlowLayoutPanel
{
    readonly System.Windows.Forms.Timer motion = new()
    {
        Interval = Motion.FrameMilliseconds
    };
    Dictionary<Control, (int From, int To)> positions = [];
    long started;
    bool suspended;
    public AnimatedAccountList()
    {
        DoubleBuffered = true;
        motion.Tick += (_, _) =>
        {
            double p = Math.Clamp((Environment.TickCount64 - started) / Motion.ReorderMilliseconds, 0, 1);
            double eased = Motion.EaseOut(p);
            foreach (var (row, move) in positions)
                row.Top = (int)(move.From + (move.To - move.From) * eased);
            if (p >= 1)
                FinishMotion();
        };
    }

    public void MoveRow(AccountEditorRow row, int index)
    {
        if (Controls.GetChildIndex(row) == index)
            return;
        var old = Controls.Cast<Control>().ToDictionary(c => c, c => c.Top);
        FinishMotion();
        Controls.SetChildIndex(row, index);
        PerformLayout();
        positions = Controls.Cast<Control>().ToDictionary(c => c, c => (old[c], c.Top));
        SuspendLayout();
        suspended = true;
        foreach (var (control, move) in positions)
            control.Top = move.From;
        started = Environment.TickCount64;
        motion.Start();
    }

    public int TargetTop(Control row) => motion.Enabled && positions.TryGetValue(row, out var move) ? move.To : row.Top;
    public void FinishMotion()
    {
        motion.Stop();
        if (suspended)
        {
            suspended = false;
            ResumeLayout(true);
            PerformLayout();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            motion.Dispose();
        base.Dispose(disposing);
    }
}
