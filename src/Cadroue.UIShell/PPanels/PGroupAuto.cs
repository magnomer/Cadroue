using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PGroup
{
    private static readonly Brush pGroupActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private Border pGroupActionHost = null!;
    private bool pGroupAuto;
    private bool pGroupStrict = true;

    public bool PGroupAutoCheck() => pGroupAuto;

    public bool PGroupStrictCheck() => pGroupStrict;

    public void PGroupModeRestore(bool pGroupSeedAuto, bool pGroupSeedStrict)
    {
        pGroupAuto = pGroupSeedAuto;
        pGroupStrict = pGroupSeedStrict;
        PGroupActionUpdate();
        PGroupAutoUpdate();
    }

    public void PGroupAutoUpdate()
    {
        if (!pGroupAuto)
        {
            return;
        }

        PGroupAutoApply(pGroupStrict);
        PGroupSort();
    }

    private UIElement PGroupActionBuild()
    {
        pGroupActionHost = new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White
        };
        PGroupActionUpdate();
        return pGroupActionHost;
    }

    private void PGroupActionUpdate() => pGroupActionHost.Child = PGroupContentBuild();

    private void PGroupModeSet(bool pGroupModeAuto)
    {
        if (pGroupAuto == pGroupModeAuto)
        {
            return;
        }

        pGroupAuto = pGroupModeAuto;
        PGroupActionUpdate();
        PGroupAutoUpdate();
    }

    private void PGroupStrictSet(bool pGroupSwitchStrict)
    {
        if (pGroupStrict == pGroupSwitchStrict)
        {
            return;
        }

        pGroupStrict = pGroupSwitchStrict;
        PGroupActionUpdate();
        PGroupAutoUpdate();
    }

    private UIElement PGroupContentBuild()
    {
        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6), MinHeight = 26 };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Border pModeToggle = PGroupToggleBuild(
            LLocalization.LLocalizationTextRead("Group.Manual.Label"),
            LLocalization.LLocalizationTextRead("Group.Manual.Tooltip"),
            !pGroupAuto,
            () => PGroupModeSet(false),
            LLocalization.LLocalizationTextRead("Group.Auto.Label"),
            LLocalization.LLocalizationTextRead("Group.Auto.Tooltip"),
            pGroupAuto,
            () => PGroupModeSet(true));
        pModeToggle.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetColumn(pModeToggle, 0);
        pActionGrid.Children.Add(pModeToggle);

        UIElement pTrailing = pGroupAuto ? PGroupSwitchBuild() : PGroupManualBuild();
        Grid.SetColumn(pTrailing, 2);
        pActionGrid.Children.Add(pTrailing);
        return pActionGrid;
    }

    private Border PGroupSwitchBuild()
    {
        Border pStrictToggle = PGroupToggleBuild(
            LLocalization.LLocalizationTextRead("Group.Strict.Label"),
            LLocalization.LLocalizationTextRead("Group.Strict.Tooltip"),
            pGroupStrict,
            () => PGroupStrictSet(true),
            LLocalization.LLocalizationTextRead("Group.Loose.Label"),
            LLocalization.LLocalizationTextRead("Group.Loose.Tooltip"),
            !pGroupStrict,
            () => PGroupStrictSet(false));
        pStrictToggle.HorizontalAlignment = HorizontalAlignment.Right;
        pStrictToggle.Margin = new Thickness(8, 0, 0, 0);
        return pStrictToggle;
    }

    private StackPanel PGroupManualBuild()
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(PGroupButtonBuild(
            "/PAssets/PPanels/PGroupStrict.svg",
            LLocalization.LLocalizationTextRead("Group.Strict.Tooltip"),
            (_, _) => PGroupStrictApply()));
        pButtons.Children.Add(PGroupButtonBuild(
            "/PAssets/PPanels/PGroupLoose.svg",
            LLocalization.LLocalizationTextRead("Group.Loose.Tooltip"),
            (_, _) => PGroupLooseApply()));
        Button pSortButton = PGroupButtonBuild(
            "/PAssets/PPanels/PSort.svg",
            LLocalization.LLocalizationTextRead("Group.Sort.Tooltip"),
            (_, _) => PGroupSort());
        pSortButton.Margin = new Thickness(0);
        pButtons.Children.Add(pSortButton);
        return pButtons;
    }

    private Border PGroupToggleBuild(
        string pGroupLeftText,
        string pGroupLeftTip,
        bool pGroupLeftActive,
        Action pGroupLeftClick,
        string pGroupRightText,
        string pGroupRightTip,
        bool pGroupRightActive,
        Action pGroupRightClick)
    {
        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(PGroupSegmentBuild(pGroupLeftText, pGroupLeftTip, pGroupLeftActive, pGroupLeftClick));
        pRow.Children.Add(new Border { Width = 1, Background = pGroupLineBrush });
        pRow.Children.Add(PGroupSegmentBuild(pGroupRightText, pGroupRightTip, pGroupRightActive, pGroupRightClick));

        return new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            Child = pRow
        };
    }

    private static Border PGroupSegmentBuild(string pGroupText, string pGroupTip, bool pGroupActive, Action pGroupClick)
    {
        var pLabel = new TextBlock
        {
            Text = pGroupText,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = pGroupActive ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = pGroupActive ? pGroupTitleBrush : pGroupMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pSegment = new Border
        {
            Background = pGroupActive ? pGroupActiveBrush : Brushes.Transparent,
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = Cursors.Hand,
            ToolTip = pGroupTip,
            Child = pLabel
        };
        pSegment.MouseLeftButtonUp += (_, _) => pGroupClick();
        return pSegment;
    }

    private void PGroupStrictApply() => PGroupAutoApply(true);

    private void PGroupLooseApply() => PGroupAutoApply(false);

    private void PGroupAutoApply(bool pGroupStrict)
    {
        IReadOnlyList<string> pFiles = PGroupSourceFiles?.Invoke() ?? Array.Empty<string>();
        IReadOnlyList<LSeriesGroup> pGroups = LSeries.LSeriesResolve(pFiles, pGroupStrict);

        pGroupRecords.Clear();
        foreach (LSeriesGroup pGroupSeries in pGroups)
        {
            var pRecord = new PGroupRecord { PGroupRecordName = pGroupSeries.Name };
            pRecord.PGroupRecordPaths.AddRange(pGroupSeries.Paths);
            pGroupRecords.Add(pRecord);
        }

        PGroupRebuild();
    }
}
