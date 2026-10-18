using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PExportPresetBox
{
    private readonly QExport lExport;
    private TextBox? pExportBoxCurrent;

    public PExportPresetBox(QExport lExport) => this.lExport = lExport;

    internal void PExportEditCommit()
    {
        if (lExport.QExportEditing is not string lEditingName || pExportBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        PExportNameCommit(lEditingName, pEditingBox.Text);
    }

    private void PExportNameCommit(string lOldPresetName, string lNewPresetName)
    {
        if (!lExport.QExportEditingCheck(lOldPresetName))
        {
            return;
        }

        PExportBoxSet(null);
        lExport.QExportNameCommit(lOldPresetName, lNewPresetName);
    }

    internal TextBox PExportBoxBuild(string lPresetName)
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
        Foreground = PExportTheme.PExportTextBrush,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0, 0, 0, 1),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
        Padding = new Thickness(2, 0, 2, 1),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Center,
        FocusVisualStyle = null
    };

    internal void PExportBoxSet(TextBox? pNameBox) => pExportBoxCurrent = pNameBox;

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

    private static bool PExportInsideCheck(DependencyObject? pSource, DependencyObject pTarget)
    {
        while (pSource is not null)
        {
            if (ReferenceEquals(pSource, pTarget))
            {
                return true;
            }

            pSource = pSource is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(pSource)
                : LogicalTreeHelper.GetParent(pSource);
        }

        return false;
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
            lExport.QExportEditCancel();
            return true;
        }

        return false;
    }
}
