using System.Windows;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QNameplateTree
{
    public static string QNameplateTreeRead(DependencyObject lStart)
    {
        DependencyObject? lOwner = QWalk.QWalkParentFind(
            VisualTreeHelper.GetParent(lStart),
            lNode => QNameplate.QNameplateOwnerCheck((lNode as FrameworkElement)?.Name, lNode.GetType().Namespace));
        return QNameplate.QNameplateResolve(
            (lStart as FrameworkElement)?.Name,
            lStart.GetType().Name,
            (lOwner as FrameworkElement)?.Name,
            lOwner?.GetType().Name);
    }
}
