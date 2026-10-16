using System.Windows;

namespace Cadroue.UIDeportment;

public static class QWalkType
{
    public static bool QWalkTypeMatch(DependencyObject lNode, IReadOnlyList<Type> lTypes) =>
        lTypes.Any(lNode.GetType().IsAssignableTo);
}
