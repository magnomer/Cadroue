using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PExport
{
    private void PExportEditCommit()
    {
        if (pPresetNameEditing is not string lEditingName || pExportBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        PExportNameCommit(lEditingName, pEditingBox.Text);
    }

    private UIElement PExportDisplayBuild(string lPresetName, bool pPresetModified, bool pPresetUnsupported)
    {
        UIElement pNameText = PExportNameBuild(lPresetName, pPresetModified, pPresetUnsupported);
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

    private UIElement PExportNameBuild(string lPresetName, bool pPresetModified, bool pPresetUnsupported)
    {
        TextBlock pNameText = PExportTextBuild(lPresetName, pPresetModified, pPresetUnsupported);
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

    private static TextBlock PExportTextBuild(string lPresetName, bool pPresetModified, bool pPresetUnsupported) => new()
    {
        Text = PExportMarkRead(
            pPresetModified
                ? $"{LPreset.LPresetDisplayRead(lPresetName)} (Modified)"
                : LPreset.LPresetDisplayRead(lPresetName),
            pPresetUnsupported),
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
