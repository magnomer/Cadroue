using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public static class QMenu
{
    private const string QMenuStyle = "PMenu";

    private const string QMenuBelowStyle = "PMenuBelow";

    private const string QMenuAboveStyle = "PMenuAbove";

    public static ContextMenu QMenuBuild() => new()
    {
        Style = QContract.QContractSheetFind<Style>(QMenuStyle),
    };

    public static ContextMenu QMenuBuild(UIElement lTarget, QMenuSide lSide) => new()
    {
        PlacementTarget = lTarget,
        Style = lSide switch
        {
            QMenuSide.QMenuSideBelow => QContract.QContractSheetFind<Style>(QMenuBelowStyle),
            QMenuSide.QMenuSideAbove => QContract.QContractSheetFind<Style>(QMenuAboveStyle),
            _ => throw new ArgumentOutOfRangeException(nameof(lSide)),
        },
    };
}
