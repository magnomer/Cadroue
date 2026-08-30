using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;
using Cadroue.UIShell.PSShared;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell;

/// <summary>
/// Developer-only catalog for exercising windows and dialogs without arranging
/// the application state that normally causes them.
/// </summary>
internal sealed class PSDebug : Window
{
    private const double PSDebugWidth = 780;
    private const double PSDebugHeight = 640;

    private static PSDebug? psDebugCurrent;

    private readonly Window psDebugApplicationOwner;

    private enum PSDebugKind
    {
        PSDebugAnnouncement,
        PSDebugWarning,
        PSDebugAlert,
        PSDebugDecision,
        PSDebugChoice
    }

    private sealed record PSDebugMessage(
        string PSDebugName,
        string PSDebugMessageText,
        PSDebugKind PSDebugKind);

    private static readonly PSDebugMessage[] PSDebugMessages =
    {
        new("About: resource open failure", "The requested link or resource could not be opened.\n\nDebug detail: simulated failure.", PSDebugKind.PSDebugWarning),
        new("Options: settings save failure", "Settings could not be saved.\n\nDebug detail: simulated failure.", PSDebugKind.PSDebugWarning),
        new("Options: language restart required", "Restart Cadroue to finish changing the language.", PSDebugKind.PSDebugAnnouncement),
        new("System: Flyleaf installation result", "Flyleaf installation finished. This is a simulated result.", PSDebugKind.PSDebugAnnouncement),
        new("System: no file records", "There are no file records to remove.", PSDebugKind.PSDebugAnnouncement),
        new("System: clear file records", "Remove all saved file records?", PSDebugKind.PSDebugAlert),
        new("System: file records removed", "3 file records were removed.", PSDebugKind.PSDebugAnnouncement),
        new("System: clear completed work", "Remove all completed work records?", PSDebugKind.PSDebugAlert),
        new("System: completed work removed", "3 completed work records were removed.", PSDebugKind.PSDebugAnnouncement),
        new("System: workspace running", "The workspace cannot be reset while work is running.", PSDebugKind.PSDebugWarning),
        new("System: reset workspace", "Reset the entire workspace?", PSDebugKind.PSDebugAlert),
        new("System: workspace reset", "The workspace was reset.", PSDebugKind.PSDebugAnnouncement),
        new("System: MPV installation result", "MPV installation finished. This is a simulated result.", PSDebugKind.PSDebugAnnouncement),
        new("Timeline: invalid palette", "The selected palette is invalid.", PSDebugKind.PSDebugWarning),
        new("Timeline: remove palette", "Remove the selected palette?", PSDebugKind.PSDebugAlert),
        new("LosslessCut: detected project", "A LosslessCut project was detected. Use it for this import?", PSDebugKind.PSDebugDecision),
        new("LosslessCut: missing media", "The project media could not be found.", PSDebugKind.PSDebugAnnouncement),
        new("LosslessCut: read failure", "The LosslessCut project could not be read.", PSDebugKind.PSDebugWarning),
        new("LosslessCut: version warning", "This project was created by a different version. Continue?", PSDebugKind.PSDebugDecision),
        new("LosslessCut: media mismatch", "The selected media does not match the project. Continue?", PSDebugKind.PSDebugDecision),
        new("LosslessCut: empty project", "The project contains no segments to import.", PSDebugKind.PSDebugAnnouncement),
        new("LosslessCut: import mode", "Import segments as one tab or as separate tabs?", PSDebugKind.PSDebugChoice),
        new("Preset: write failure", "The export preset could not be written.", PSDebugKind.PSDebugWarning),
        new("Preset: read failure", "The export preset could not be read.", PSDebugKind.PSDebugWarning),
        new("Preset: invalid import", "The imported export preset is invalid.", PSDebugKind.PSDebugWarning),
        new("Preset: missing preset", "The selected export preset no longer exists.", PSDebugKind.PSDebugAnnouncement),
        new("Source: unsupported audio", "This audio-only file is not supported by this source field.", PSDebugKind.PSDebugAnnouncement),
        new("Sidecar: read failure", "The sidecar file could not be read.", PSDebugKind.PSDebugWarning),
        new("Sidecar: source mismatch (load)", "The sidecar belongs to another source. Load it anyway?", PSDebugKind.PSDebugDecision),
        new("Sidecar: source mismatch (save)", "The existing sidecar belongs to another source. Replace it?", PSDebugKind.PSDebugAlert),
        new("Tabs: busy worklist", "This tab cannot be moved while its worklist is busy.", PSDebugKind.PSDebugAnnouncement),
        new("Schedule: cancel running jobs", "Cancel the running jobs?", PSDebugKind.PSDebugAlert),
        new("Schedule: destructive clear", "Permanently clear the selected jobs or tabs?", PSDebugKind.PSDebugAlert),
        new("Scene: missing name", "Enter a scene name before saving.", PSDebugKind.PSDebugAnnouncement),
        new("Scene: load confirmation", "Loading this scene will replace the current workspace. Continue?", PSDebugKind.PSDebugAlert),
        new("Scene: operation error", "The scene operation failed.\n\nDebug detail: simulated failure.", PSDebugKind.PSDebugWarning),
        new("Roster: remove jobs", "Permanently remove the selected jobs?", PSDebugKind.PSDebugAlert),
        new("Log: operation error", "The log operation failed.\n\nDebug detail: simulated failure.", PSDebugKind.PSDebugWarning)
    };

    internal static void PSDebugShow(Window pOwner)
    {
        if (psDebugCurrent is not null)
        {
            psDebugCurrent.Activate();
            return;
        }

        psDebugCurrent = new PSDebug(pOwner);
        psDebugCurrent.Show();
    }

    private PSDebug(Window pOwner)
    {
        psDebugApplicationOwner = pOwner;
        Title = "Debug";
        Owner = pOwner;
        Width = PSDebugWidth;
        Height = PSDebugHeight;
        MinWidth = 520;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        FontSize = PSField.PSFieldFontSize;
        Content = PSDebugBuild();
        Closed += (_, _) => psDebugCurrent = null;
    }

    private UIElement PSDebugBuild()
    {
        var pRoot = new StackPanel { Margin = new Thickness(18) };
        pRoot.Children.Add(PSDebugHeadingBuild("Subwindows"));

        var pWindows = PSDebugWrapBuild();
        PSDebugButtonAdd(pWindows, "About", () => PSAbout.PSAboutShow(this));
        PSDebugButtonAdd(pWindows, "Options", () => PSOptions.PSOptionsShow(this, null));
        PSDebugButtonAdd(pWindows, "Shortcuts", () => PSKeymap.PSKeymapShow(this, null));
        PSDebugButtonAdd(pWindows, "Log", () => PLogWindow.PLogWindowShow(psDebugApplicationOwner));
        PSDebugButtonAdd(pWindows, "Diagnosis", () => PSDiagnosis.PSDiagnosisShow(this));
        PSDebugButtonAdd(pWindows, "Encoder", PSDebugEncoderShow);
        PSDebugButtonAdd(pWindows, "Verdict", () => PSVerdict.PSVerdictShow(this, "Debug verdict", new[]
        {
            new PSVerdictRow("Video", "debug-pass", true, "Simulated successful verification."),
            new PSVerdictRow("Audio", "debug-fail", false, "Simulated verification detail.")
        }));
        PSDebugButtonAdd(pWindows, "Loupe", () => PSLoupe.PSLoupeShow(this, new PViewer()));
        PSDebugButtonAdd(pWindows, "Monitor", PSDebugMonitorShow);
        PSDebugButtonAdd(pWindows, "Destructive alert", () => PSAlert.PSAlertConfirm(this, "Debug alert", "Exercise the custom destructive confirmation window?", "Confirm"));
        PSDebugButtonAdd(pWindows, "Decision", () => PSDecision.PSDecisionSelect(this, "Debug decision", "Exercise the neutral multi-choice decision window?", "Primary", "Alternate", "Cancel"));
        pRoot.Children.Add(pWindows);

        pRoot.Children.Add(PSDebugHeadingBuild("Message boxes"));
        var pMessages = PSDebugWrapBuild();
        foreach (PSDebugMessage pMessage in PSDebugMessages)
        {
            PSDebugMessage pCurrent = pMessage;
            PSDebugButtonAdd(pMessages, pCurrent.PSDebugName, () => PSDebugMessageShow(pCurrent));
        }
        pRoot.Children.Add(pMessages);

        return new ScrollViewer
        {
            Content = pRoot,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private void PSDebugMessageShow(PSDebugMessage pMessage)
    {
        switch (pMessage.PSDebugKind)
        {
            case PSDebugKind.PSDebugWarning:
                PSWarning.PSWarningShow(this, pMessage.PSDebugName, pMessage.PSDebugMessageText);
                break;
            case PSDebugKind.PSDebugAlert:
                PSAlert.PSAlertConfirm(
                    this,
                    pMessage.PSDebugName,
                    pMessage.PSDebugMessageText,
                    LLocalization.LLocalizationTextRead("Terms.Delete"));
                break;
            case PSDebugKind.PSDebugDecision:
                PSDecision.PSDecisionConfirm(
                    this,
                    pMessage.PSDebugName,
                    pMessage.PSDebugMessageText,
                    LLocalization.LLocalizationTextRead("Terms.Continue"),
                    LLocalization.LLocalizationTextRead("Terms.Cancel"));
                break;
            case PSDebugKind.PSDebugChoice:
                PSDecision.PSDecisionSelect(
                    this,
                    pMessage.PSDebugName,
                    pMessage.PSDebugMessageText,
                    LLocalization.LLocalizationTextRead("Terms.Replace"),
                    LLocalization.LLocalizationTextRead("Terms.Append"),
                    LLocalization.LLocalizationTextRead("Terms.Cancel"));
                break;
            default:
                PSAnnouncement.PSAnnouncementShow(this, pMessage.PSDebugName, pMessage.PSDebugMessageText);
                break;
        }
    }

    private void PSDebugEncoderShow()
    {
        var pEncoder = new PSEncoder(new LPreset(), () => { }) { Owner = this };
        pEncoder.ShowDialog();
    }

    private void PSDebugMonitorShow()
    {
        var pSource = new LSMonitor();
        var pFlow = new PFlowControl();
        var pViewer = new PViewer();
        Window pMonitor = PSMonitor.PSMonitorShow(this, pSource, pFlow, pViewer);
        pMonitor.Closed += (_, _) => pSource.Dispose();
    }

    private static TextBlock PSDebugHeadingBuild(string pText) => new()
    {
        Text = pText,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Foreground = PSField.PSFieldText,
        Margin = new Thickness(0, 4, 0, 10)
    };

    private static WrapPanel PSDebugWrapBuild() => new()
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 0, 0, 18)
    };

    private static void PSDebugButtonAdd(Panel pPanel, string pText, Action pAction)
    {
        var pButton = new Button
        {
            Content = pText,
            MinWidth = 150,
            MinHeight = 34,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
        pButton.Click += (_, _) => pAction();
        pPanel.Children.Add(pButton);
    }
}
