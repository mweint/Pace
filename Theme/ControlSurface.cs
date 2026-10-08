using Avalonia.Controls.Templates;

namespace Pace;

// Painted controls still need a full transparent surface for pointer hit-testing.
internal static class ControlSurface
{
    public static FuncControlTemplate<T> Template<T>() where T : Avalonia.Controls.Primitives.TemplatedControl =>
        new((_, _) => new Border { Background = Brushes.Transparent });
}
