using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Usage;

public static class TrayToggle
{
    public static bool ShouldClose(bool visible, long lastAutoHide, long now) => visible || (lastAutoHide > 0 && now - lastAutoHide < 350);
}
