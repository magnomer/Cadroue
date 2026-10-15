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
        bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = QExport.QExportSelectedCheck(lPresetName);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPreset.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = QExport.QExportEditingCheck(lPresetName);
        bool pPresetUnsupported = !QExport.QExportSupportCheck(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? PExportBoxBuild(lPresetName)
            : PExportDisplayBuild(lPresetName, pPresetModified, pPresetUnsupported);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 5, 12, 5),
            Background = pPresetSelected ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Opacity = pPresetUnsupported ? 0.42 : 1,
            ToolTip = pPresetUnsupported ? QLocalizationCatalog.QLocalizationTextRead(
                QExport.QExportSupportNotice) : null,
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
                QExport.QExportEditStart(lPresetName);
                pEvent.Handled = true;
                return;
            }

            if (pPresetNative || PExportSourceCheck(pEvent.OriginalSource))
            {
                pExportDragOrigin = null;
                QExport.QExportDragClear();
                return;
            }

            Point pGrabPoint = pEvent.GetPosition(pRowBorder);
            QExport.QExportDragStart(lPresetName, pGrabPoint.X, pGrabPoint.Y);
            pPresetRowDragging = pRowBorder;
            pPresetOpacityOrigin = pRowBorder.Opacity;
            pExportDragOrigin = pEvent.GetPosition(pPresetRowPanel);
            pPresetRowPanel.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonUp += (_, pEvent) =>
        {
            if (pPresetEditing)
            {
                return;
            }

            if (!QExport.QExportEditingCheck(lPresetName))
            {
                PExportEditCommit();
                PExportPresetSelect(lPresetName);
            }

            pEvent.Handled = true;
        };
        return pRowBorder;
    }
}
