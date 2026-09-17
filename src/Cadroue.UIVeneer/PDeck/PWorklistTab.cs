using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;

namespace Cadroue.UIVeneer.PDeck;

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
