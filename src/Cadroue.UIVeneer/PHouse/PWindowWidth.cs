using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIVeneer.PToolbar;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private void PWindowWidthAttach(PDeck.PTabSurface pWindowSurface)
    {
        pWindowSurfaceActive = pWindowSurface;
        pWindowSurfaceActive.PTabWidthChange += PWindowWidthHandle;
        if (pListActive is not null)
        {
            pListActive.PListMinimizeChange += PWindowListHandle;
        }

        PWindowWidthApply();
    }

    private void PWindowWidthDetach()
    {
        if (pWindowSurfaceActive is not null)
        {
            pWindowSurfaceActive.PTabWidthChange -= PWindowWidthHandle;
            pWindowSurfaceActive = null;
        }

        if (pListActive is not null)
        {
            pListActive.PListMinimizeChange -= PWindowListHandle;
        }
    }

    private void PWindowListHandle(bool pWindowListMinimized) => PWindowWidthApply();

    private void PWindowWidthHandle() => PWindowWidthApply();

    private void PWindowWidthApply()
    {
        double pWindowRequired = pWindowSurfaceActive?.PTabWidthRead() ?? 0;
        double pWindowContentMinimum = Math.Max(PWindowWidthFloor, pWindowRequired);
        double pWindowReservedWidth = pTabRailColumn.Width.IsAbsolute
            ? pTabRailColumn.Width.Value
            : 0;
        MinWidth = pWindowContentMinimum + pWindowReservedWidth;
        if (Width < MinWidth)
        {
            Width = MinWidth;
        }
    }

}
