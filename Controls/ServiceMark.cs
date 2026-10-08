using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Pace;

public static class ServiceMark
{
    static readonly Bitmap Claude = Load("claude"), Codex = Load("codex");
    static readonly Dictionary<int, Bitmap> SmallCodex = new[] { 16, 20, 24, 32 }.ToDictionary(size => size, size => Load($"codex-{size}"));
    static Bitmap Load(string name) => new(AssetLoader.Open(new Uri($"avares://Pace/Assets/Icons/{name}.png")));
    public static void Draw(DrawingContext context, string service, Point position, double scale = 1)
    {
        var artwork = service == Services.Claude ? Claude : SmallCodex.GetValueOrDefault((int)Math.Round(UiMetrics.ServiceIconSize * scale), Codex);
        context.DrawImage(artwork, new Rect(position, new Size(UiMetrics.ServiceIconSize, UiMetrics.ServiceIconSize)));
    }
}
internal sealed class ServiceIcon(string service) : Control
{
    protected override Size MeasureOverride(Size availableSize) => new(UiMetrics.ServiceIconSize, UiMetrics.ServiceIconSize);
    public override void Render(DrawingContext context) => ServiceMark.Draw(context, service, default, TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
}
