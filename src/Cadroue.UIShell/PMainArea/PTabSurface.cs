using System.Windows.Controls;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public abstract class PTabSurface : UserControl
{
    public abstract PFlowControl? PTabFlow { get; }
    public abstract PViewer? PTabViewer { get; }
}
