using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Cadroue.UIDeportment;

// The grab guard: a press or a hover inside an interactive control never grabs the window.
public static class QSashGuard
{
    private static readonly Type[] QSashGuardTypes =
    [
        typeof(ButtonBase),
        typeof(TextBoxBase),
        typeof(PasswordBox),
        typeof(Selector),
        typeof(RangeBase),
        typeof(Thumb),
        typeof(MenuItem),
        typeof(Hyperlink),
    ];

    public static bool QSashGuardCheck(DependencyObject? lSource) =>
        QWalk.QWalkParentCheck(lSource, QSashGuardMatch);

    private static bool QSashGuardMatch(DependencyObject lNode) =>
        QWalkType.QWalkTypeMatch(lNode, QSashGuardTypes);
}
