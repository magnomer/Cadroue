using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QMenu
{
    private const string QMenuStyle = "PMenu";

    private const string QMenuBelowStyle = "PMenuBelow";

    private const string QMenuAboveStyle = "PMenuAbove";

    private const string QMenuItemStyle = "PMenuItem";

    private const string QMenuItemIcon = "PMenuItemIcon";

    private const string QMenuInkSheet = "PMenuInk";

    public static ImageSource QMenuIconRead(string lPath)
        => QIcon.QIconRead(lPath, QContract.QContractSheetFind<Brush>(QMenuInkSheet));

    public static MenuItem QMenuItemBuild(string lText) => QMenuItemBuild(lText, null);

    public static MenuItem QMenuItemBuild(string lText, ImageSource? lIcon)
    {
        MenuItem lItem = QMenuItemCreate(lText, lIcon);
        QMenuItemAttach(lItem);
        return lItem;
    }

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

    private static MenuItem QMenuItemCreate(string lText, ImageSource? lIcon)
    {
        MenuItem lItem = new()
        {
            Header = lText,
            Style = QContract.QContractSheetFind<Style>(QMenuItemStyle),
        };
        if (lIcon is not null)
        {
            Image lImage = QContract.QContractSheetFind<Image>(QMenuItemIcon);
            lImage.Source = lIcon;
            lItem.Icon = lImage;
        }

        return lItem;
    }

    private static void QMenuItemAttach(MenuItem lItem)
    {
        lItem.AddHandler(
            MenuItem.ClickEvent,
            new RoutedEventHandler(QInteractionClick.QInteractionClickHandle),
            handledEventsToo: true);
        QLook.QLookAttach(lItem, QMenuItemStyle);
    }
}
