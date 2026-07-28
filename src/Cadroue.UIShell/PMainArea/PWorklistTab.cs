using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PWorklistTab : PTabSurface
{
    private readonly PRoster pRoster;

    public PWorklistTab(LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        pRoster = new PRoster(lPreferenceTabLayout);
        Content = pRoster;
    }

    public override PFlowControl? PTabFlow => null;
    public override PViewer? PTabViewer => null;
    public override bool PTabBusyCheck() => pRoster.PRosterBusyCheck();
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => pRoster.PRosterLayoutRead();
}
