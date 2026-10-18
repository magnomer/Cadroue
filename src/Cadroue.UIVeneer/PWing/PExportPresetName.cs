using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private void PExportEditCommit()
    {
        if (QExport.QExportEditing is not string lEditingName || pExportBoxCurrent is not { } pEditingBox)
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
        pButtonPanel.Children.Add(PExportInlineBuild(
            PExportCheckIcon,
            PExportApplyBrush,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ApplyTooltip"),
            PExportModificationApply));
        pButtonPanel.Children.Add(PExportInlineBuild(
            PExportCancelIcon,
            PExportCancelBrush,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.DiscardTooltip"),
            PExportModificationRestore));
        Grid.SetColumn(pButtonPanel, 1);
        pGrid.Children.Add(pButtonPanel);
        return pGrid;
    }

    private UIElement PExportNameBuild(string lPresetName, bool pPresetModified, bool pPresetUnsupported)
    {
        TextBlock pNameText = PExportTextBuild(lPresetName, pPresetModified, pPresetUnsupported);
        if (!LPresetTable.LPresetNativeCheck(lPresetName))
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

    private static TextBlock PExportTextBuild(
        string lPresetName,
        bool pPresetModified,
        bool pPresetUnsupported)
    {
        string pTitle = PExportTitleFormat(lPresetName, pPresetModified, pPresetUnsupported);
        FontStyle pStyle = PExportStyleResolve(pPresetModified);
        return PExportLabelBuild(pTitle, pStyle);
    }

    private static string PExportTitleFormat(string lPresetName, bool pPresetModified, bool pPresetUnsupported) =>
        PExportMarkRead(
            pPresetModified
                ? $"{LPresetNaming.LPresetDisplayRead(lPresetName)} (Modified)"
                : LPresetNaming.LPresetDisplayRead(lPresetName),
            pPresetUnsupported);

    private static FontStyle PExportStyleResolve(bool pPresetModified) =>
        pPresetModified ? FontStyles.Italic : FontStyles.Normal;

    private static TextBlock PExportLabelBuild(string pTitle, FontStyle pStyle) => new()
    {
        Text = pTitle,
        FontSize = 12,
        FontStyle = pStyle,
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
            Text = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Native"),
            FontSize = 10,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x5E, 0x75)),
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private TextBox PExportBoxBuild(string lPresetName)
    {
        TextBox pNameBox = PExportFieldBuild(lPresetName);
        PExportBoxSet(pNameBox);
        PExportBoxAttach(pNameBox, lPresetName);
        return pNameBox;
    }

    private static TextBox PExportFieldBuild(string lPresetName) => new()
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

    private void PExportBoxSet(TextBox? pNameBox) => pExportBoxCurrent = pNameBox;

    private void PExportBoxAttach(TextBox pNameBox, string lPresetName)
    {
        Window? pEditWindow = null;
        MouseButtonEventHandler pOutsideHandle = (_, pDownEvent) =>
            PExportOutsideHandle(pNameBox, lPresetName, pDownEvent);
        pNameBox.Loaded += (_, _) => pEditWindow = PExportOpenHandle(pNameBox, pOutsideHandle);
        pNameBox.Unloaded += (_, _) => PExportWindowDetach(pEditWindow, pOutsideHandle);
        pNameBox.LostKeyboardFocus += (_, _) => PExportFocusHandle(pNameBox, lPresetName);
        pNameBox.KeyDown += (_, pEvent) =>
            pEvent.Handled = PExportKeyHandle(pNameBox, lPresetName, pEvent.Key) || pEvent.Handled;
    }

    private void PExportOutsideHandle(TextBox pNameBox, string lPresetName, MouseButtonEventArgs pDownEvent)
    {
        if (!ReferenceEquals(pExportBoxCurrent, pNameBox)
            || PExportInsideCheck(pDownEvent.OriginalSource as DependencyObject, pNameBox))
        {
            return;
        }

        PExportNameCommit(lPresetName, pNameBox.Text);
    }

    private static Window? PExportOpenHandle(TextBox pNameBox, MouseButtonEventHandler pOutsideHandle)
    {
        pNameBox.Focus();
        pNameBox.SelectAll();
        return PExportWindowAttach(pNameBox, pOutsideHandle);
    }

    private static Window? PExportWindowAttach(TextBox pNameBox, MouseButtonEventHandler pOutsideHandle)
    {
        Window? pEditWindow = Window.GetWindow(pNameBox);
        if (pEditWindow is not null)
        {
            pEditWindow.PreviewMouseDown += pOutsideHandle;
        }

        return pEditWindow;
    }

    private static void PExportWindowDetach(Window? pEditWindow, MouseButtonEventHandler pOutsideHandle)
    {
        if (pEditWindow is not null)
        {
            pEditWindow.PreviewMouseDown -= pOutsideHandle;
        }
    }

    private void PExportFocusHandle(TextBox pNameBox, string lPresetName)
    {
        if (!ReferenceEquals(pExportBoxCurrent, pNameBox))
        {
            return;
        }

        PExportNameCommit(lPresetName, pNameBox.Text);
    }

    private bool PExportKeyHandle(TextBox pNameBox, string lPresetName, Key pKey)
    {
        if (pKey == Key.Return)
        {
            PExportNameCommit(lPresetName, pNameBox.Text);
            return true;
        }

        if (pKey == Key.Escape)
        {
            PExportBoxSet(null);
            QExport.QExportEditCancel();
            return true;
        }

        return false;
    }
}
