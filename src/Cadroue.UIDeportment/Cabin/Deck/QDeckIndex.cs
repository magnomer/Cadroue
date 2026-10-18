using System.Runtime.CompilerServices;
using System.Windows;

namespace Cadroue.UIDeportment;

// The deck's built surface of each tab, read back only while the tab is still in the strip.
public sealed class QDeckIndex
{
    private readonly QStrip qStrip;
    private readonly ConditionalWeakTable<QStripTab, FrameworkElement> qDeckSurfaces = new();

    internal QDeckIndex(QStrip lStripOwner)
    {
        qStrip = lStripOwner;
    }

    public void QDeckSurfaceAdd(QStripTab lStripTab, FrameworkElement lSurface) =>
        qDeckSurfaces.Add(lStripTab, lSurface);

    public TSurface? QDeckSurfaceRead<TSurface>(QStripTab? lStripTab)
        where TSurface : FrameworkElement =>
        lStripTab is not null && qStrip.QStripTabs.Contains(lStripTab)
            && qDeckSurfaces.TryGetValue(lStripTab, out var lSurface)
                ? lSurface as TSurface
                : null;
}
