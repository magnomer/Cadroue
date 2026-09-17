using System;
using System.Windows;
using System.Windows.Controls;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer
{
    public event Action<bool>? PViewerBypassChange;

    public void PViewerAudioSet(bool pAudioOnlyAllowed) => LViewer.LViewerAllowSet(pAudioOnlyAllowed);

    public void PViewerAudioSet(string pViewerGraph)
    {
        LViewer.LViewerFilterSet(pViewerGraph);
        PViewerAudioApply();
        LViewer.LViewerPreviewRaise();
    }

    public bool PViewerBypassRead() => LViewer.LViewerBypass;

    public void PViewerBypassSet(bool pBypass) => LViewer.LViewerBypassSet(pBypass);

    private void PViewerBypassHandle(bool pBypass)
    {
        PViewerAudioUpdate();
        PViewerAudioApply();
        PViewerBypassChange?.Invoke(pBypass);
        LViewer.LViewerPreviewRaise();
    }

    private void PViewerAudioToggle() => PViewerBypassSet(!LViewer.LViewerBypass);

    private void PViewerAudioApply()
    {
        if (!LViewer.LViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        string pViewerEffective = LViewer.LViewerAudioResolve();
        if (pViewerEffective == LPlayer.LPlayerAudioApplied)
        {
            return;
        }

        try
        {
            pViewerPlayer.PPlayerAudioSet(pViewerEffective);
            LPlayer.LPlayerAudioSet(pViewerEffective);
        }
        catch (Exception pViewerAudioException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected audio filter '{pViewerEffective}': {pViewerAudioException.Message}");
        }
    }

    private Button PViewerAudioBuild()
    {
        var pButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 52, 0, 0),
            MinWidth = 84,
            Height = 24,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            Visibility = Visibility.Collapsed,
            Style = Cadroue.UIVeneer.PHouse.PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PViewerAudioToggle();
        return pButton;
    }

    private void PViewerAudioUpdate()
    {
        bool pViewerAudioCapable = PViewerEngineCurrent == LPreviewEngine.LPreviewEngineMpv;
        pViewerAudioSwitch.IsEnabled = pViewerAudioCapable;
        pViewerAudioSwitch.Content = LLocalization.LLocalizationTextRead(
            LViewer.LViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered");
        pViewerAudioSwitch.ToolTip = LLocalization.LLocalizationTextRead(
            pViewerAudioCapable ? "Viewer.Audio.SwitchTooltip" : "Viewer.Audio.MpvRequired");
    }

    private void PViewerAudioShow(bool pViewerAudioVisible)
    {
        pViewerAudioSwitch.Visibility = pViewerAudioVisible && LViewer.LViewerAudioEligible
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (pViewerAudioVisible && LViewer.LViewerAudioEligible)
        {
            PViewerAudioUpdate();
        }
    }
}
