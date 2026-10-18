using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PWorklistTab : PTabSurface
{
    private readonly PRoster pRoster;

    public PWorklistTab(QRoster lRoster, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pRoster = new PRoster(lRoster, lPreferenceTabLayout);
        Content = pRoster;
    }

    public override void PTabClose() => pRoster.PRosterClose();

    public override PFlow? PTabFlow => null;
    public override PViewer? PTabViewer => null;
    public override double PTabWidthRead() => pRoster.PRosterWidthRead();
    public override LSceneTabEntry PTabLayoutRead() => pRoster.PRosterLayoutRead();
}
