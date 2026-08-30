using System;
using System.Windows;
using System.Windows.Controls;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private string pViewerAudioFilter = string.Empty;
    private string? pViewerAudioApplied;
    private bool pViewerBypass;

    public event Action<bool>? PViewerBypassChange;

    public bool PViewerAudioEligible { get; set; }

    public void PViewerAudioSet(string pViewerGraph)
    {
        pViewerAudioFilter = pViewerGraph ?? string.Empty;
        PViewerAudioApply();
        PViewerPreviewChange?.Invoke();
    }

    public bool PViewerBypassRead() => pViewerBypass;

    public void PViewerBypassSet(bool pBypass)
    {
        if (pViewerBypass == pBypass)
        {
            return;
        }

        pViewerBypass = pBypass;
        PViewerAudioUpdate();
        PViewerAudioApply();
        PViewerBypassChange?.Invoke(pViewerBypass);
        PViewerPreviewChange?.Invoke();
    }

    private string PViewerAudioResolve() =>
        pViewerBypass ? string.Empty : pViewerAudioFilter;

    private void PViewerAudioToggle() => PViewerBypassSet(!pViewerBypass);

    private void PViewerAudioApply()
    {
        if (!pViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        string pViewerEffective = PViewerAudioResolve();
        if (pViewerEffective == pViewerAudioApplied)
        {
            return;
        }

        try
        {
            pViewerPlayer.PPlayerAudioSet(pViewerEffective);
            pViewerAudioApplied = pViewerEffective;
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
            Style = Cadroue.UIShell.PMainWindow.PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PViewerAudioToggle();
        return pButton;
    }

    private void PViewerAudioUpdate()
    {
        bool pViewerAudioCapable = PViewerEngineCurrent == LPreviewEngine.LPreviewEngineMpv;
        pViewerAudioSwitch.IsEnabled = pViewerAudioCapable;
        pViewerAudioSwitch.Content = LLocalization.LLocalizationTextRead(
            pViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered");
        pViewerAudioSwitch.ToolTip = LLocalization.LLocalizationTextRead(
            pViewerAudioCapable ? "Viewer.Audio.SwitchTooltip" : "Viewer.Audio.MpvRequired");
    }

    private void PViewerAudioShow(bool pViewerAudioVisible)
    {
        pViewerAudioSwitch.Visibility = pViewerAudioVisible && PViewerAudioEligible
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (pViewerAudioVisible && PViewerAudioEligible)
        {
            PViewerAudioUpdate();
        }
    }
}
