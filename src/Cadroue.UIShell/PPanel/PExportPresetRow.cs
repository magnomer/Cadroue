using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PExport
{
    private Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPreset.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = string.Equals(lPresetName, pPresetNameEditing, StringComparison.OrdinalIgnoreCase);
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

    // Whether this row's preset can carry the work of the tab hosting the panel. The row
    // stays selectable either way; an unsupported one is only marked, and refused when the
    // user actually runs it. The selected row is judged by the working copy, which is what
    // would run, so editing it back into range clears the mark at once.
    private bool PExportSupportCheck(string lPresetName, LPreset lWorking)
    {
        if (pExportKind is not { } pExportWorkKind)
        {
            return true;
        }

        LPreset? pPresetValue = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase)
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

    // The refusal the action itself makes, over the selection that would actually be sent.
    public static bool PExportSupportCheck(LPresetSelection lPresetOwner, LWorkKind lExportKind) =>
        lPresetOwner.LPresetSelectionEncoding is not { } lExportEncoding
        || lExportEncoding.LEncodingSupportCheck(lExportKind);
}
