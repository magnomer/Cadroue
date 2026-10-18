using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PExportPresetRow
{
    private readonly QExport lExport;
    private readonly StackPanel pPresetRowPanel;
    private readonly PExportPresetBox pExportBox;
    private readonly PExportPresetDrag pExportDrag;
    private readonly PExportPresetCommand pExportCommand;
    private readonly Action<string> pPresetClick;

    public PExportPresetRow(
        QExport lExport,
        StackPanel pPresetRowPanel,
        PExportPresetBox pExportBox,
        PExportPresetDrag pExportDrag,
        PExportPresetCommand pExportCommand,
        Action<string> pPresetClick)
    {
        this.lExport = lExport;
        this.pPresetRowPanel = pPresetRowPanel;
        this.pExportBox = pExportBox;
        this.pExportDrag = pExportDrag;
        this.pExportCommand = pExportCommand;
        this.pPresetClick = pPresetClick;
    }

    internal Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        (bool pPresetNative, bool pPresetSelected, bool pPresetModified, bool pPresetEditing, bool pPresetUnsupported) =
            PExportFlagResolve(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? pExportBox.PExportBoxBuild(lPresetName)
            : PExportPresetName.PExportDisplayBuild(
                lPresetName,
                pPresetModified,
                pPresetUnsupported,
                pExportCommand.PExportModificationApply,
                pExportCommand.PExportModificationRestore);
        (Brush pFill, double pOpacity) = PExportShadeResolve(pPresetSelected, pPresetUnsupported);
        Border pRowBorder = PExportBorderBuild(pNameElement, pFill, pOpacity, PExportTipRead(pPresetUnsupported));
        PExportRowAttach(pRowBorder, lPresetName, pPresetNative, pPresetEditing);
        return pRowBorder;
    }

    private (bool pPresetNative, bool pPresetSelected, bool pPresetModified, bool pPresetEditing,
        bool pPresetUnsupported) PExportFlagResolve(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPresetTable.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = lExport.QExportSelectedCheck(lPresetName);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPresetTable.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = lExport.QExportEditingCheck(lPresetName);
        bool pPresetUnsupported = !lExport.QExportSupportCheck(lPresetName, lWorking);
        return (pPresetNative, pPresetSelected, pPresetModified, pPresetEditing, pPresetUnsupported);
    }

    private static (Brush pPresetFill, double pPresetOpacity) PExportShadeResolve(
        bool pPresetSelected, bool pPresetUnsupported) =>
        (pPresetSelected ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            pPresetUnsupported ? 0.42 : 1);

    private string? PExportTipRead(bool pPresetUnsupported) =>
        pPresetUnsupported ? QLocalizationCatalog.QLocalizationTextRead(lExport.QExportSupportNotice) : null;

    private static Border PExportBorderBuild(
        UIElement pNameElement, Brush pFill, double pOpacity, string? pTip) => new()
    {
        Padding = new Thickness(12, 5, 12, 5),
        Background = pFill,
        BorderBrush = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Opacity = pOpacity,
        ToolTip = pTip,
        Child = pNameElement
    };

    private void PExportRowAttach(Border pRowBorder, string lPresetName, bool pPresetNative, bool pPresetEditing)
    {
        pRowBorder.PreviewMouseLeftButtonDown += (_, pEvent) =>
            pEvent.Handled = PExportDownHandle(pRowBorder, lPresetName, pPresetNative, pPresetEditing, pEvent)
                || pEvent.Handled;
        pRowBorder.MouseLeftButtonUp += (_, pEvent) =>
            pEvent.Handled = PExportClickHandle(lPresetName, pPresetEditing) || pEvent.Handled;
    }

    private bool PExportDownHandle(
        Border pRowBorder,
        string lPresetName,
        bool pPresetNative,
        bool pPresetEditing,
        MouseButtonEventArgs pEvent)
    {
        if (pPresetEditing)
        {
            return false;
        }

        if (!pPresetNative && pEvent.ClickCount >= 2)
        {
            pExportDrag.PExportDragClear();
            pPresetRowPanel.ReleaseMouseCapture();
            lExport.QExportEditStart(lPresetName);
            return true;
        }

        if (pPresetNative || PExportSourceCheck(pEvent.OriginalSource))
        {
            pExportDrag.PExportOriginSet(null);
            lExport.QExportDrag.QExportDragClear();
            return false;
        }

        Point pGrabPoint = pEvent.GetPosition(pRowBorder);
        lExport.QExportDrag.QExportDragStart(lPresetName, pGrabPoint.X, pGrabPoint.Y);
        pExportDrag.PExportDragSet(pRowBorder);
        pExportDrag.PExportOriginSet(pEvent.GetPosition(pPresetRowPanel));
        pPresetRowPanel.CaptureMouse();
        return false;
    }

    private static bool PExportSourceCheck(object pSource)
    {
        DependencyObject? pObject = pSource as DependencyObject;
        while (pObject is not null)
        {
            if (pObject is Button)
            {
                return true;
            }

            pObject = VisualTreeHelper.GetParent(pObject);
        }

        return false;
    }

    private bool PExportClickHandle(string lPresetName, bool pPresetEditing)
    {
        if (pPresetEditing)
        {
            return false;
        }

        if (!lExport.QExportEditingCheck(lPresetName))
        {
            pExportBox.PExportEditCommit();
            pPresetClick(lPresetName);
        }

        return true;
    }
}
