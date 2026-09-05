using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIShell.PToolbar;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PHouse;

public partial class PWindow
{
    private void PWindowOptionsHandle(LPreferenceState lPreferenceState)
    {
        FontSize = PWindowFontSize;
        PWindowLayoutApply(lPreferenceState.LPreferenceVerticalTabs);
        if (pFlowActive is not null)
        {
            pFlowActive.Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
            pFlowActive.PFlowOrderApply();
            pFlowActive.PFlowPaletteApply();
        }
        PWindowVolumeSync(lPreferenceState);
    }

    private void PWindowLayoutApply(bool pVertical)
    {
        UIElement pSceneControls = pConsole.PConsoleSceneRead();
        pToolbar.PToolbarTabSet(null);
        pTabRailHost.Content = null;
        pConsole.PConsoleSceneSet(null);
        pToolbar.PToolbarSceneSet(null);

        pRail.PRailApply(pVertical);
        pToolbar.PToolbarVerticalSet(pVertical);
        pTabRailColumn.Width = new GridLength(pVertical ? PRail.PRailWidth : 0);
        if (pVertical)
        {
            pTabRailHost.Content = pRail;
            pToolbar.PToolbarSceneSet(pSceneControls);
        }
        else
        {
            pToolbar.PToolbarTabSet(pRail);
            pConsole.PConsoleSceneSet(pSceneControls);
        }

        PWindowWidthApply();
    }

}
