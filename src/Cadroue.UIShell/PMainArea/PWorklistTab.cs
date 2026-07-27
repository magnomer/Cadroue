using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PWorklistTab : PTabSurface
{
    public PWorklistTab()
    {
        Content = new System.Windows.Controls.Grid
        {
            Children =
            {
                new PRoster()
            }
        };
    }

    public override PFlowControl? PTabFlow => null;
    public override PViewer? PTabViewer => null;
}
