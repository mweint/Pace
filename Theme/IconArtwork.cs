using System.Xml.Linq;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Pace;

// Lucide icons drawn from their packaged SVG geometry and stroked with the theme's icon weight,
// so they stay crisp at every scale and can rotate.
internal static class IconArtwork
{
    const double ViewBox = 24;
    static readonly Dictionary<string, Geometry> shapes = [];
    static readonly Dictionary<Color, IPen> pens = [];

    public static void Draw(DrawingContext context, string name, Rect bounds, Color ink, double degrees = 0)
    {
        double scale = Math.Min(bounds.Width, bounds.Height) / ViewBox;
        var transform = Matrix.CreateTranslation(-ViewBox / 2, -ViewBox / 2) * Matrix.CreateRotation(Math.PI * degrees / 180)
            * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(bounds.Center.X, bounds.Center.Y);
        using (context.PushTransform(transform))
            context.DrawGeometry(null, Pen(ink), Shape(name));
    }

    internal static Geometry Shape(string name)
    {
        if (shapes.TryGetValue(name, out var shape))
            return shape;
        using var stream = AssetLoader.Open(new Uri($"avares://Pace/Assets/Icons/{name}.svg"));
        var group = new GeometryGroup();
        foreach (var element in XDocument.Load(stream).Descendants())
        {
            if (element.Name.LocalName == "path" && (string?)element.Attribute("d") is { } data)
                group.Children.Add(Geometry.Parse(data));
            else if (element.Name.LocalName == "circle")
            {
                double x = (double)element.Attribute("cx")!, y = (double)element.Attribute("cy")!, r = (double)element.Attribute("r")!;
                group.Children.Add(new EllipseGeometry(new Rect(x - r, y - r, 2 * r, 2 * r)));
            }
        }
        return shapes[name] = group;
    }

    static IPen Pen(Color ink)
    {
        if (!pens.TryGetValue(ink, out var pen))
            pens[ink] = pen = new ImmutablePen((IImmutableBrush)Palette.Brush(ink), UiMetrics.IconStroke, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        return pen;
    }
}
