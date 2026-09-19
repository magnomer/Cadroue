using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PWorklistTab : PTabSurface
{
    private readonly PRoster pRoster;

    public PWorklistTab(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pRoster = new PRoster(lPreferenceTabLayout);
        Content = pRoster;
    }

    public override void PTabClose() => pRoster.PRosterClose();

    public override PFlow? PTabFlow => null;
    public override PViewer? PTabViewer => null;
    public override LStation? PTabStation => pRoster.PRosterStation;
    public override double PTabWidthRead() => pRoster.PRosterWidthRead();
    public override LSceneTabRecord PTabLayoutRead() => pRoster.PRosterLayoutRead();
}
