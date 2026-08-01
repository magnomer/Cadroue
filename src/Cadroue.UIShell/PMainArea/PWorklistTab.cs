using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PWorklistTab : PTabSurface
{
    private readonly PRoster pRoster;

    public PWorklistTab(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pRoster = new PRoster(lPreferenceTabLayout);
        Content = pRoster;
    }

    public override void PTabClose() => pRoster.PRosterClose();

    public override PFlowControl? PTabFlow => null;
    public override PViewer? PTabViewer => null;
    public override bool PTabBusyCheck() => pRoster.PRosterBusyCheck();
    public override double PTabWidthRead() => pRoster.PRosterWidthRead();
    public override LSceneTabRecord PTabLayoutRead() => pRoster.PRosterLayoutRead();
}
