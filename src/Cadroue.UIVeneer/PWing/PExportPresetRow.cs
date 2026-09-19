using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;
using Cadroue.Core;

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
        bool pPresetUnsupported = !PExportSupportCheck(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? PExportBoxBuild(lPresetName)
            : PExportDisplayBuild(lPresetName, pPresetModified, pPresetUnsupported);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pPresetSelected ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Opacity = pPresetUnsupported ? 0.42 : 1,
            ToolTip = pPresetUnsupported ? LLocalization.LLocalizationTextRead(PExportNoticeRead()) : null,
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

            LExport.LExportDragStart(lPresetName);
            pPresetRowDragging = pRowBorder;
            pPresetOpacityOrigin = pRowBorder.Opacity;
            pExportDragOrigin = pEvent.GetPosition(pPresetRowPanel);
            pPresetDragOffset = pEvent.GetPosition(pRowBorder);
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

    private bool PExportSupportCheck(string lPresetName, LPreset lWorking)
    {
        if (pExportKind is not { } pExportWorkKind)
        {
            return true;
        }

        LPreset? pPresetValue = LExport.LExportSelectedCheck(lPresetName)
            ? lWorking
            : LPreset.LPresetRead(lPresetName);
        return pPresetValue is not { } pPreset
            || LEncoding.LEncodingSupportCheck(
                pExportWorkKind,
                pPreset.LPresetVideo.LPresetMode,
                pPreset.LPresetAudio.LPresetMode,
                pPreset.LPresetAudio.LPresetStream);
    }

    private string PExportNoticeRead() =>
        pExportKind == LWorkKind.LWorkKindEdit ? "ExportPreset.DisabledTooltip" : "ExportPreset.AudioTooltip";

    public static bool PExportSupportCheck(LPresetSelection lPresetOwner, LWorkKind lExportKind) =>
        lPresetOwner.LPresetSelectionEncoding is not { } lExportEncoding
        || lExportEncoding.LEncodingSupportCheck(lExportKind);
}
