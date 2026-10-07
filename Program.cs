namespace Usage;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-test"))
        {
            SelfTests.Run(args.Last());
            return;
        }

        if (args.Contains("--live-check"))
        {
            LiveCheck.Run(args.Last());
            return;
        }

        if (args.Contains("--render-preview"))
        {
            Preview.Render(args.Last());
            return;
        }

        using var mutex = new Mutex(true, "Local\\Pace.TrayApp.V1", out bool created);
        if (!created)
            return;
        Application.Run(new TrayApp());
    }
}
