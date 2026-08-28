using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private const string PExportPlusIcon = "/PAssets/PPanels/PExportPlus.svg";
    private const string PExportMinusIcon = "/PAssets/PPanels/PExportMinus.svg";
    private const string PExportSettingIcon = "/PAssets/PPanels/PExportSetting.svg";
    private const string PExportImportIcon = "/PAssets/PPanels/PExportImport.svg";
    private const string PExportExportIcon = "/PAssets/PPanels/PExportExport.svg";
    private const string PExportUserGroupPreference = "$User";
    private const string PExportCheckIcon = "/PAssets/PPanels/PExportCheck.svg";
    private const string PExportCancelIcon = "/PAssets/PPanels/PExportCancel.svg";
    private const string PExportCollapseIcon = "/PAssets/PPanels/PExportCollapse.svg";
    private const string PExportExpandIcon = "/PAssets/PPanels/PExportExpand.svg";
    private static readonly Brush PExportApplyBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA3, 0x66));
    private static readonly Brush PExportCancelBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x43, 0x43));

    private UIElement PExportPresetBuild()
    {
        var pScroll = new ScrollViewer
        {
            Content = pPresetRowPanel,
            Background = Brushes.White,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null
        };

        PExportPresetRebuild();
        return pScroll;
    }

    private void PExportPresetRebuild()
    {
        LPreset lWorking = PExportWorkingRead();
        pPresetRebuilding = true;
        try
        {
            pPresetRowPanel.Children.Clear();
            string? pNativeGroupCurrent = null;
            bool pUserHeaderAdded = false;
            foreach (string lPresetName in LPreset.LPresetNames)
            {
                bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
                string? pGroupName = pPresetNative ? LPreset.LPresetGroupRead(lPresetName) : null;
                if (pPresetNative
                    && pGroupName is not null
                    && !string.Equals(pNativeGroupCurrent, pGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    pPresetRowPanel.Children.Add(PExportGroupBuild(
                        pGroupName,
                        LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(pGroupName),
                        () => PExportGroupToggle(pGroupName)));
                    pNativeGroupCurrent = pGroupName;
                }
                else if (!pPresetNative && !pUserHeaderAdded)
                {
                    pPresetRowPanel.Children.Add(PExportGroupBuild(
                        LLocalization.LLocalizationTextRead("ExportPreset.Group.User"),
                        LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(
                            PExportUserGroupPreference,
                            false),
                        PExportUserToggle));
                    pUserHeaderAdded = true;
                }

                Border pRow = PExportRowBuild(lPresetName, lWorking);
                bool pCollapsed = pPresetNative && pGroupName is not null
                    ? LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(pGroupName)
                    : LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(
                        PExportUserGroupPreference,
                        false);
                pRow.Visibility = pCollapsed ? Visibility.Collapsed : Visibility.Visible;
                pPresetRowPanel.Children.Add(pRow);
            }
        }
        finally
        {
            pPresetRebuilding = false;
        }
    }

    private Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPreset.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = string.Equals(lPresetName, pPresetNameEditing, StringComparison.OrdinalIgnoreCase);
        bool pPresetDisabled = PExportDisabledCheck(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? PExportBoxBuild(lPresetName)
            : PExportDisplayBuild(lPresetName, pPresetModified);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pPresetSelected && !pPresetDisabled ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Opacity = pPresetDisabled ? 0.42 : 1,
            ToolTip = pPresetDisabled ? LLocalization.LLocalizationTextRead("ExportPreset.DisabledTooltip") : null,
            Child = pNameElement
        };
        pRowBorder.PreviewMouseLeftButtonDown += (_, pEvent) =>
        {
            if (pPresetEditing)
            {
                return;
            }

            if (!pPresetNative && pEvent.ClickCount >= 2)
            {
                PExportDragClear();
                pPresetRowPanel.ReleaseMouseCapture();
                if (!string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase))
                {
                    PExportPresetSelect(lPresetName);
                }

                pPresetNameEditing = lPresetName;
                PExportPresetRebuild();
                pEvent.Handled = true;
                return;
            }

            if (pPresetNative || PExportSourceCheck(pEvent.OriginalSource))
            {
                pPresetNameDragging = null;
                pExportDragOrigin = null;
                return;
            }

            pPresetNameDragging = lPresetName;
            pPresetRowDragging = pRowBorder;
            pPresetRowOpacity = pRowBorder.Opacity;
            pExportDragOrigin = pEvent.GetPosition(pPresetRowPanel);
            pPresetDragOffset = pEvent.GetPosition(pRowBorder);
            pPresetDragActive = false;
            pPresetRowPanel.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonUp += (_, pEvent) =>
        {
            if (pPresetEditing)
            {
                return;
            }

            if (!string.Equals(pPresetNameEditing, lPresetName, StringComparison.OrdinalIgnoreCase))
            {
                PExportEditCommit();
                PExportPresetSelect(lPresetName);
            }

            pEvent.Handled = true;
        };
        return pRowBorder;
    }

    private bool PExportDisabledCheck(string lPresetName, LPreset lWorking) =>
        pExportCopyDisabled
        && LPreset.LPresetRead(lPresetName) is { } lPreset
        && string.Equals(lPreset.LPresetVideo.LPresetMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && (!string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(lWorking.LPresetVideo.LPresetMode, "Copy", StringComparison.OrdinalIgnoreCase));

    private Border PExportGroupBuild(string pLabel, bool pCollapsed, Action pToggle)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = PExportMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });

        var pToggleButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead(pCollapsed ? PExportExpandIcon : PExportCollapseIcon, PExportMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = LLocalization.LLocalizationTextRead(pCollapsed
                ? "ExportPreset.Group.Expand"
                : "ExportPreset.Group.Collapse"),
            Width = 22,
            Height = 20,
            Style = PExportStyleRead()
        };
        pToggleButton.Click += (_, _) => pToggle();
        Grid.SetColumn(pToggleButton, 1);
        pGrid.Children.Add(pToggleButton);

        return new Border
        {
            Tag = "Header",
            Padding = new Thickness(12, 5, 8, 5),
            Background = Brushes.White,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pGrid
        };
    }

    private void PExportGroupToggle(string pGroupName)
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(pGroupName);
        LPreference.LPreferencePresetGroupFoldedSet(pGroupName, !pCollapsed);
        PExportPresetRebuild();
    }

    private void PExportUserToggle()
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferencePresetGroupFoldedRead(
            PExportUserGroupPreference,
            false);
        LPreference.LPreferencePresetGroupFoldedSet(PExportUserGroupPreference, !pCollapsed, false);
        PExportPresetRebuild();
    }

    private void PExportEditCommit()
    {
        if (pPresetNameEditing is not string lEditingName || pExportBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        PExportNameCommit(lEditingName, pEditingBox.Text);
    }

    private void PExportPresetSelect(string lPresetName)
    {
        pPresetNameSelected = lPresetName;
        lPresetOwner.LPresetSelectionSelect(lPresetName);
    }

    private UIElement PExportDisplayBuild(string lPresetName, bool pPresetModified)
    {
        UIElement pNameText = PExportNameBuild(lPresetName, pPresetModified);
        if (!pPresetModified)
        {
            return pNameText;
        }

        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pNameText, 0);
        pGrid.Children.Add(pNameText);

        var pButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pButtonPanel.Children.Add(PExportInlineBuild(PExportCheckIcon, PExportApplyBrush, LLocalization.LLocalizationTextRead("ExportPreset.ApplyTooltip"), PExportModificationApply));
        pButtonPanel.Children.Add(PExportInlineBuild(PExportCancelIcon, PExportCancelBrush, LLocalization.LLocalizationTextRead("ExportPreset.DiscardTooltip"), PExportModificationRestore));
        Grid.SetColumn(pButtonPanel, 1);
        pGrid.Children.Add(pButtonPanel);
        return pGrid;
    }

    private UIElement PExportNameBuild(string lPresetName, bool pPresetModified)
    {
        TextBlock pNameText = PExportTextBuild(lPresetName, pPresetModified);
        if (!LPreset.LPresetNativeCheck(lPresetName))
        {
            return pNameText;
        }

        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pPanel.Children.Add(pNameText);
        pPanel.Children.Add(PExportBadgeBuild());
        return pPanel;
    }

    private static TextBlock PExportTextBuild(string lPresetName, bool pPresetModified) => new()
    {
        Text = pPresetModified
            ? $"{LPreset.LPresetDisplayRead(lPresetName)} (Modified)"
            : LPreset.LPresetDisplayRead(lPresetName),
        FontSize = 12,
        FontStyle = pPresetModified ? FontStyles.Italic : FontStyles.Normal,
        Foreground = PExportTextBrush,
        Padding = new Thickness(2, 0, 2, 1),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border PExportBadgeBuild() => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF6)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xD4, 0xE2)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(3),
        Padding = new Thickness(5, 1, 5, 2),
        Margin = new Thickness(7, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("ExportPreset.Native"),
            FontSize = 10,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x5E, 0x75)),
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private Button PExportInlineBuild(string pIconPath, Brush pIconBrush, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 13,
                Height = 13,
                Source = PIcon.PIconRead(pIconPath, pIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 22,
            Height = 20,
            Margin = new Thickness(2, 0, 0, 0),
            Style = PExportStyleRead()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private TextBox PExportBoxBuild(string lPresetName)
    {
        var pNameBox = new TextBox
        {
            Text = lPresetName,
            FontSize = 12,
            Foreground = PExportTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Padding = new Thickness(2, 0, 2, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };
        pExportBoxCurrent = pNameBox;
        Window? pEditWindow = null;
        MouseButtonEventHandler pOutsideHandle = (_, pDownEvent) =>
        {
            if (pPresetRebuilding || PExportInsideCheck(pDownEvent.OriginalSource as DependencyObject, pNameBox))
            {
                return;
            }

            PExportNameCommit(lPresetName, pNameBox.Text);
        };
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
            pEditWindow = Window.GetWindow(pNameBox);
            if (pEditWindow is not null)
            {
                pEditWindow.PreviewMouseDown += pOutsideHandle;
            }
        };
        pNameBox.Unloaded += (_, _) =>
        {
            if (pEditWindow is not null)
            {
                pEditWindow.PreviewMouseDown -= pOutsideHandle;
            }
        };
        pNameBox.LostKeyboardFocus += (_, _) =>
        {
            if (pPresetRebuilding)
            {
                return;
            }

            PExportNameCommit(lPresetName, pNameBox.Text);
        };
        pNameBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PExportNameCommit(lPresetName, pNameBox.Text);
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                pPresetNameEditing = null;
                pExportBoxCurrent = null;
                PExportPresetRebuild();
                pEvent.Handled = true;
            }
        };
        return pNameBox;
    }
}
