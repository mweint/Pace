using System.Collections.Specialized;

namespace Pace;

// A multi-child drawing control. Layout subclasses own geometry; this base owns
// attaching children to Avalonia's logical and visual trees and the section surface.
public abstract class PaintedPanel : Control
{
    public Avalonia.Controls.Controls Children { get; } = new();
    protected PaintedPanel()
    {
        Children.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
            }
            else
            {
                if (e.OldItems != null)
                    foreach (Control child in e.OldItems) { LogicalChildren.Remove(child); VisualChildren.Remove(child); }
                if (e.NewItems != null)
                {
                    int index = e.NewStartingIndex;
                    foreach (Control child in e.NewItems) { LogicalChildren.Insert(index, child); VisualChildren.Insert(index++, child); }
                }
            }
            InvalidateMeasure();
        };
    }
    public sealed override void Render(DrawingContext context)
    {
        context.FillRectangle(Palette.Brush(Palette.SectionBackground), new Rect(Bounds.Size));
        DrawSurface(context);
    }
    protected virtual void DrawSurface(DrawingContext context) { }
}
