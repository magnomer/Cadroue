using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterComparison
{
    private const string PRosterOpenIcon = "/PAsset/PPanel/PRosterOpen.svg";

    public static UIElement PRosterComparisonBuild(QRoster lRoster, QRosterDetail lDetail)
    {
        (StackPanel pSourceStack, StackPanel pOutputStack) =
            PRosterOverview.PRosterStacksBuild(lDetail.QRosterDetailCompares);
        pSourceStack.Children.Insert(0, PRosterOpenBuild(lRoster, lDetail.QRosterDetailSource));
        pOutputStack.Children.Insert(0, PRosterOpenBuild(lRoster, lDetail.QRosterDetailOutput));

        var pRoot = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterOverview.PRosterGridBuild(pSourceStack, pOutputStack));
        pRoot.Children.Add(PRosterSoundBuild(lDetail));
        pRoot.Children.Add(PRosterOverview.PRosterDividerBuild());
        pRoot.Children.Add(
            PRosterOverview.PRosterPathsBuild(lDetail.QRosterDetailSources, lDetail.QRosterDetailOutputs, 8));
        return pRoot;
    }

    private static UIElement PRosterSoundBuild(QRosterDetail lDetail)
    {
        (StackPanel pSourceStack, StackPanel pOutputStack) =
            PRosterOverview.PRosterStacksBuild(lDetail.QRosterDetailSounds);
        Grid pGrid = PRosterOverview.PRosterGridBuild(pSourceStack, pOutputStack);
        pGrid.Margin = new Thickness(0, 8, 0, 0);
        var pBlock = new StackPanel { Visibility = PLook.PLookVisible[lDetail.QRosterDetailSound] };
        pBlock.Children.Add(PRosterOverview.PRosterDividerBuild());
        pBlock.Children.Add(pGrid);
        return pBlock;
    }

    private static Button PRosterOpenBuild(QRoster lRoster, string pPath)
    {
        Button pButton = PRosterLinkBuild();
        PRosterOpenAttach(pButton, lRoster, pPath);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static void PRosterOpenAttach(Button pButton, QRoster lRoster, string pPath) =>
        pButton.Click += (_, _) => lRoster.QRosterPathOpen(pPath);

    private static Button PRosterLinkBuild()
    {
        return new Button
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = QIcon.QIconRead(PRosterOpenIcon, PRosterTheme.PRosterTextBrush),
                Stretch = Stretch.Uniform
            },
            Width = 20,
            Height = PRosterTheme.PRosterRowHeight,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Roster.Explorer.Tooltip")
        };
    }
}
