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
    private Border PExportRowBuild(string lPresetName, LPreset lWorking)
    {
        bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
        bool pPresetSelected = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase);
        bool pPresetModified = !pPresetNative
            && pPresetSelected
            && !LPreset.LPresetMatch(lPresetName, lWorking);
        bool pPresetEditing = string.Equals(lPresetName, pPresetNameEditing, StringComparison.OrdinalIgnoreCase);
        bool pPresetDisabled = PExportDisabledCheck(lPresetName, lWorking);
        UIElement pNameElement = pPresetEditing
            ? PExportBoxBuild(lPresetName)
            : PExportDisplayBuild(lPresetName, pPresetModified);

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

    private bool PExportDisabledCheck(string lPresetName, LPreset lWorking) =>
        pExportCopyDisabled
        && LPreset.LPresetRead(lPresetName) is { } lPreset
        && string.Equals(lPreset.LPresetVideo.LPresetMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && (!string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(lWorking.LPresetVideo.LPresetMode, "Copy", StringComparison.OrdinalIgnoreCase));

}
