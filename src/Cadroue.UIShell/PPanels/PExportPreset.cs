using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private const string PExportPlusIconPath = "/PAssets/PPanels/PExportPlus.svg";
    private const string PExportMinusIconPath = "/PAssets/PPanels/PExportMinus.svg";
    private const string PExportSettingIconPath = "/PAssets/PPanels/PExportSetting.svg";
    private const string PExportImportIconPath = "/PAssets/PPanels/PExportImport.svg";
    private const string PExportExportIconPath = "/PAssets/PPanels/PExportExport.svg";
    private const string PExportCheckIconPath = "/PAssets/PPanels/PExportCheck.svg";
    private const string PExportCancelIconPath = "/PAssets/PPanels/PExportCancel.svg";
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
        pPresetRebuilding = true;
        try
        {
            pPresetRowPanel.Children.Clear();
            bool pUserDividerAdded = false;
            foreach (string lPresetName in LExportSpecificState.LPresetNames)
            {
                if (!LExportSpecificState.LPresetNativeCheck(lPresetName)
                    && pPresetRowPanel.Children.Count > 0
                    && !pUserDividerAdded)
                {
                    pPresetRowPanel.Children.Add(PExportPresetDividerBuild());
                    pUserDividerAdded = true;
                }

                pPresetRowPanel.Children.Add(PExportPresetRowBuild(lPresetName));
            }
        }
        finally
        {
            pPresetRebuilding = false;
        }
    }

    private Border PExportPresetRowBuild(string lPresetName)
    {
        bool pPresetNative = LExportSpecificState.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LExportSpecificState.LPresetMatch(lPresetName, lExportSpecificState);
        bool pPresetEditing = string.Equals(lPresetName, pPresetNameEditing, StringComparison.OrdinalIgnoreCase);
        bool pPresetDisabled = PExportPresetDisabledCheck(lPresetName);
        UIElement pNameElement = pPresetEditing
            ? PExportPresetNameBoxBuild(lPresetName)
            : PExportPresetDisplayBuild(lPresetName, pPresetModified);

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
            if (pPresetNative || PExportButtonSourceCheck(pEvent.OriginalSource))
            {
                pPresetNameDragging = null;
                pPresetDragStart = null;
                return;
            }

            pPresetNameDragging = lPresetName;
            pPresetDragStart = pEvent.GetPosition(pRowBorder);
            pPresetDragOffset = pPresetDragStart.Value;
            pPresetDragActive = false;
            pRowBorder.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonDown += (_, pEvent) =>
        {
            if (!pPresetNative && pEvent.ClickCount >= 2)
            {
                pRowBorder.ReleaseMouseCapture();
                PExportPresetDragClear();
                PExportPresetSelect(lPresetName);
                pPresetNameEditing = lPresetName;
                PExportPresetRebuild();
                pEvent.Handled = true;
            }
        };
        pRowBorder.PreviewMouseMove += (_, pEvent) =>
        {
            if (pPresetNameDragging is null
                || pPresetEditing
                || pPresetDragStart is not Point pStart
                || pEvent.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point pCurrent = pEvent.GetPosition(pRowBorder);
            if (Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (!pPresetDragActive)
            {
                pPresetDragGhost = PGhost.PGhostShow(pRowBorder, pPresetDragOffset);
            }

            pPresetDragActive = true;
            pRowBorder.Opacity = 0.42;
            pPresetDragGhost?.PGhostCursorSync();
            int lPresetTargetIndex = PExportPresetIndexResolve(pEvent.GetPosition(pPresetRowPanel));
            if (PExportPresetMoveLive(pPresetNameDragging, lPresetTargetIndex, pRowBorder))
            {
                pPresetDragStart = pEvent.GetPosition(pRowBorder);
            }

            pEvent.Handled = true;
        };
        pRowBorder.MouseLeftButtonUp += (_, pEvent) =>
        {
            pRowBorder.ReleaseMouseCapture();
            if (pPresetDragActive && pPresetNameDragging is string lDraggedPresetName)
            {
                pRowBorder.Opacity = 1;
                PExportPresetDragClear();
                pEvent.Handled = true;
                return;
            }

            PExportPresetDragClear();

            if (!string.Equals(pPresetNameEditing, lPresetName, StringComparison.OrdinalIgnoreCase))
            {
                PExportPresetEditCommit();
                PExportPresetSelect(lPresetName);
            }

            pEvent.Handled = true;
        };
        pRowBorder.LostMouseCapture += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            pRowBorder.Opacity = 1;
            PExportPresetDragClear();
        };
        return pRowBorder;
    }

    private bool PExportPresetDisabledCheck(string lPresetName) =>
        pVideoCopyPresetDisabled
        && LExportSpecificState.LPresetRead(lPresetName) is { } lPreset
        && string.Equals(lPreset.VideoMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && (!string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(lExportSpecificState.VideoMode, "Copy", StringComparison.OrdinalIgnoreCase));

    private static Border PExportPresetDividerBuild() => new()
    {
        Tag = "Divider",
        Height = 1,
        Background = PLineBrush,
        Margin = new Thickness(12, 6, 12, 6)
    };

    private void PExportPresetEditCommit()
    {
        if (pPresetNameEditing is not string lEditingName || pPresetNameBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        PExportPresetNameCommit(lEditingName, pEditingBox.Text);
    }

    private void PExportPresetSelect(string lPresetName)
    {
        pPresetNameSelected = lPresetName;
        PExportPresetApply();
    }

    private UIElement PExportPresetDisplayBuild(string lPresetName, bool pPresetModified)
    {
        UIElement pNameText = PExportPresetNameBuild(lPresetName, pPresetModified);
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
        pButtonPanel.Children.Add(PExportPresetInlineButtonBuild(PExportCheckIconPath, PExportApplyBrush, LLocalization.LLocalizationTextRead("ExportPreset.ApplyTooltip"), PExportModificationApply));
        pButtonPanel.Children.Add(PExportPresetInlineButtonBuild(PExportCancelIconPath, PExportCancelBrush, LLocalization.LLocalizationTextRead("ExportPreset.DiscardTooltip"), PExportModificationRestore));
        Grid.SetColumn(pButtonPanel, 1);
        pGrid.Children.Add(pButtonPanel);
        return pGrid;
    }

    private UIElement PExportPresetNameBuild(string lPresetName, bool pPresetModified)
    {
        TextBlock pNameText = PExportPresetNameTextBuild(lPresetName, pPresetModified);
        if (!LExportSpecificState.LPresetNativeCheck(lPresetName))
        {
            return pNameText;
        }

        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pPanel.Children.Add(pNameText);
        pPanel.Children.Add(PExportPresetDefaultBadgeBuild());
        return pPanel;
    }

    private static TextBlock PExportPresetNameTextBuild(string lPresetName, bool pPresetModified) => new()
    {
        Text = pPresetModified
            ? $"{LExportSpecificState.LPresetDisplayNameRead(lPresetName)} (Modified)"
            : LExportSpecificState.LPresetDisplayNameRead(lPresetName),
        FontSize = 12,
        FontStyle = pPresetModified ? FontStyles.Italic : FontStyles.Normal,
        Foreground = PTextBrush,
        Padding = new Thickness(2, 0, 2, 1),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border PExportPresetDefaultBadgeBuild() => new()
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

    private Button PExportPresetInlineButtonBuild(string pIconPath, Brush pIconBrush, string pTooltip, RoutedEventHandler pClick)
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
            Style = PExportButtonStyleRead()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private TextBox PExportPresetNameBoxBuild(string lPresetName)
    {
        var pNameBox = new TextBox
        {
            Text = lPresetName,
            FontSize = 12,
            Foreground = PTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Padding = new Thickness(2, 0, 2, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };
        pPresetNameBoxCurrent = pNameBox;
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        pNameBox.LostFocus += (_, _) =>
        {
            if (pPresetRebuilding)
            {
                return;
            }

            PExportPresetNameCommit(lPresetName, pNameBox.Text);
        };
        pNameBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PExportPresetNameCommit(lPresetName, pNameBox.Text);
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                pPresetNameEditing = null;
                pPresetNameBoxCurrent = null;
                PExportPresetRebuild();
                pEvent.Handled = true;
            }
        };
        return pNameBox;
    }
}
