using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PGroupAction
{
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private static readonly IReadOnlyDictionary<bool, Func<PGroupAction, UIElement>> pGroupTrailings =
        new Dictionary<bool, Func<PGroupAction, UIElement>>
        {
            [true] = pAction => pAction.PGroupSwitchesBuild(),
            [false] = pAction => pAction.PGroupManualBuild(),
        };

    private readonly QGroup lGroup;

    public PGroupAction(QGroup lGroupOwner)
    {
        lGroup = lGroupOwner;
        PGroupActionHost = PGroupHostBuild();
    }

    public Border PGroupActionHost { get; }

    public void PGroupActionUpdate() => PGroupActionHost.Child = PGroupContentBuild();

    private static Border PGroupHostBuild() => new()
    {
        BorderBrush = pGroupLineBrush,
        BorderThickness = new Thickness(0, 1, 0, 0),
        Background = Brushes.White
    };

    private UIElement PGroupContentBuild()
    {
        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6), MinHeight = 26 };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Border pModeToggle = PGroupSwitch.PGroupToggleBuild(lGroup.QGroupFace, lGroup.QGroupFace.QGroupModeRead());
        pModeToggle.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetColumn(pModeToggle, 0);
        pActionGrid.Children.Add(pModeToggle);

        UIElement pTrailing = pGroupTrailings[lGroup.QGroupAuto](this);
        Grid.SetColumn(pTrailing, 2);
        pActionGrid.Children.Add(pTrailing);
        return pActionGrid;
    }

    private StackPanel PGroupSwitchesBuild()
    {
        var pSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        lGroup.QGroupFace.QGroupSwitchesRead()
            .Select(PGroupSwitchBuild)
            .ToList()
            .ForEach(pToggle => pSwitches.Children.Add(pToggle));
        return pSwitches;
    }

    private Border PGroupSwitchBuild(QGroupSwitch lToggle)
    {
        Border pToggle = PGroupSwitch.PGroupToggleBuild(lGroup.QGroupFace, lToggle);
        pToggle.Margin = new Thickness(8, 0, 0, 0);
        return pToggle;
    }

    private StackPanel PGroupManualBuild()
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PGroupStrict.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Strict.Tooltip"),
            (_, _) => lGroup.QGroupStrictApply()));
        pButtons.Children.Add(PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PGroupLoose.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Loose.Tooltip"),
            (_, _) => lGroup.QGroupLooseApply()));
        Button pSortButton = PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PSort.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Sort.Tooltip"),
            (_, _) => lGroup.QGroupSort());
        pSortButton.Margin = new Thickness(0);
        pButtons.Children.Add(pSortButton);
        return pButtons;
    }
}
