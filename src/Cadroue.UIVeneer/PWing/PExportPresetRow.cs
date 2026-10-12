using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = LExport.LExportSelectedCheck(lPresetName);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPreset.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = LExport.LExportEditingCheck(lPresetName);
        bool pPresetUnsupported = !LExport.LExportSupportCheck(lPresetName, lWorking);
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
            ToolTip = pPresetUnsupported ? LLocalization.LLocalizationTextRead(LExport.LExportSupportNotice) : null,
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
                LExport.LExportEditStart(lPresetName);
                pEvent.Handled = true;
                return;
            }

            if (pPresetNative || PExportSourceCheck(pEvent.OriginalSource))
            {
                pExportDragOrigin = null;
                LExport.LExportDragClear();
                return;
            }

            Point pGrabPoint = pEvent.GetPosition(pRowBorder);
            LExport.LExportDragStart(lPresetName, pGrabPoint.X, pGrabPoint.Y);
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

            if (!LExport.LExportEditingCheck(lPresetName))
            {
                PExportEditCommit();
                PExportPresetSelect(lPresetName);
            }

            pEvent.Handled = true;
        };
        return pRowBorder;
    }
}
