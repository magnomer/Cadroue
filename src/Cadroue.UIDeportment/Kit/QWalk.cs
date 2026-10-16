using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Cadroue.UIDeportment;

internal static class QWalk
{
    internal static DependencyObject? QWalkParentFind(DependencyObject? lStart, Func<DependencyObject, bool> lTest)
    {
        DependencyObject? lNode = lStart;
        while (lNode is not null)
        {
            if (lTest(lNode))
            {
                return lNode;
            }

            lNode = lNode is Visual or Visual3D
                ? VisualTreeHelper.GetParent(lNode)
                : LogicalTreeHelper.GetParent(lNode);
        }

        return null;
    }
}
