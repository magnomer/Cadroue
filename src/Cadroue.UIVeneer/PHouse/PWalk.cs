using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Cadroue.UIVeneer.PHouse;

public static class PWalk
{
    public static DependencyObject? PWalkParentFind(DependencyObject? pStart, Func<DependencyObject, bool> pTest)
    {
        DependencyObject? pNode = pStart;
        while (pNode is not null)
        {
            if (pTest(pNode))
            {
                return pNode;
            }

            pNode = pNode is Visual or Visual3D
                ? VisualTreeHelper.GetParent(pNode)
                : LogicalTreeHelper.GetParent(pNode);
        }

        return null;
    }

    public static bool PWalkParentCheck(DependencyObject? pStart, Func<DependencyObject, bool> pTest) =>
        PWalkParentFind(pStart, pTest) is not null;

    public static bool PWalkTypeCheck(DependencyObject pNode, IReadOnlyList<Type> pTypes) =>
        pTypes.Any(pNode.GetType().IsAssignableTo);
}
