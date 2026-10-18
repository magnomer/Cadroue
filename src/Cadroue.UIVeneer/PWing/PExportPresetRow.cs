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
    private Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        (bool pPresetNative, bool pPresetSelected, bool pPresetModified, bool pPresetEditing, bool pPresetUnsupported) =
            PExportFlagResolve(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? PExportBoxBuild(lPresetName)
            : PExportDisplayBuild(lPresetName, pPresetModified, pPresetUnsupported);
        (Brush pFill, double pOpacity) = PExportShadeResolve(pPresetSelected, pPresetUnsupported);
        Border pRowBorder = PExportBorderBuild(pNameElement, pFill, pOpacity, PExportTipRead(pPresetUnsupported));
        PExportRowAttach(pRowBorder, lPresetName, pPresetNative, pPresetEditing);
        return pRowBorder;
    }

    private (bool pPresetNative, bool pPresetSelected, bool pPresetModified, bool pPresetEditing,
        bool pPresetUnsupported) PExportFlagResolve(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPresetTable.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = QExport.QExportSelectedCheck(lPresetName);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPresetTable.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = QExport.QExportEditingCheck(lPresetName);
        bool pPresetUnsupported = !QExport.QExportSupportCheck(lPresetName, lWorking);
        return (pPresetNative, pPresetSelected, pPresetModified, pPresetEditing, pPresetUnsupported);
    }

    private static (Brush pPresetFill, double pPresetOpacity) PExportShadeResolve(
        bool pPresetSelected, bool pPresetUnsupported) =>
        (pPresetSelected ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            pPresetUnsupported ? 0.42 : 1);

    private string? PExportTipRead(bool pPresetUnsupported) =>
        pPresetUnsupported ? QLocalizationCatalog.QLocalizationTextRead(QExport.QExportSupportNotice) : null;

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
            PExportDragClear();
            pPresetRowPanel.ReleaseMouseCapture();
            QExport.QExportEditStart(lPresetName);
            return true;
        }

        if (pPresetNative || PExportSourceCheck(pEvent.OriginalSource))
        {
            PExportOriginSet(null);
            QExport.QExportDragClear();
            return false;
        }

        Point pGrabPoint = pEvent.GetPosition(pRowBorder);
        QExport.QExportDragStart(lPresetName, pGrabPoint.X, pGrabPoint.Y);
        PExportDragSet(pRowBorder);
        PExportOriginSet(pEvent.GetPosition(pPresetRowPanel));
        pPresetRowPanel.CaptureMouse();
        return false;
    }

    private bool PExportClickHandle(string lPresetName, bool pPresetEditing)
    {
        if (pPresetEditing)
        {
            return false;
        }

        if (!QExport.QExportEditingCheck(lPresetName))
        {
            PExportEditCommit();
            PExportPresetSelect(lPresetName);
        }

        return true;
    }
}
