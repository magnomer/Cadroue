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

    private sealed record PSDebugMessage(
        string PSDebugName,
        string PSDebugMessageText,
        MessageBoxButton PSDebugButtons,
        MessageBoxImage PSDebugImage);

    private static readonly PSDebugMessage[] PSDebugMessages =
    {
        new("About: resource open failure", "The requested link or resource could not be opened.\n\nDebug detail: simulated failure.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Options: settings save failure", "Settings could not be saved.\n\nDebug detail: simulated failure.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Options: language restart required", "Restart Cadroue to finish changing the language.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: Flyleaf installation result", "Flyleaf installation finished. This is a simulated result.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: no file records", "There are no file records to remove.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: clear file records", "Remove all saved file records?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("System: file records removed", "3 file records were removed.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: clear completed work", "Remove all completed work records?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("System: completed work removed", "3 completed work records were removed.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: workspace running", "The workspace cannot be reset while work is running.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("System: reset workspace", "Reset the entire workspace?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("System: workspace reset", "The workspace was reset.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("System: MPV installation result", "MPV installation finished. This is a simulated result.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("Timeline: invalid palette", "The selected palette is invalid.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Timeline: remove palette", "Remove the selected palette?", MessageBoxButton.YesNo, MessageBoxImage.Question),
        new("LosslessCut: detected project", "A LosslessCut project was detected. Use it for this import?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question),
        new("LosslessCut: missing media", "The project media could not be found.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("LosslessCut: read failure", "The LosslessCut project could not be read.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("LosslessCut: version warning", "This project was created by a different version. Continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("LosslessCut: media mismatch", "The selected media does not match the project. Continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("LosslessCut: empty project", "The project contains no segments to import.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("LosslessCut: import mode", "Import segments as one tab or as separate tabs?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question),
        new("Preset: write failure", "The export preset could not be written.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Preset: read failure", "The export preset could not be read.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Preset: invalid import", "The imported export preset is invalid.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Preset: missing preset", "The selected export preset no longer exists.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("Source: unsupported audio", "This audio-only file is not supported by this source field.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("Sidecar: read failure", "The sidecar file could not be read.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Sidecar: source mismatch (load)", "The sidecar belongs to another source. Load it anyway?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Sidecar: source mismatch (save)", "The existing sidecar belongs to another source. Replace it?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Tabs: busy worklist", "This tab cannot be moved while its worklist is busy.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("Schedule: cancel running jobs", "Cancel the running jobs?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Schedule: destructive clear", "Permanently clear the selected jobs or tabs?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Scene: missing name", "Enter a scene name before saving.", MessageBoxButton.OK, MessageBoxImage.Information),
        new("Scene: load confirmation", "Loading this scene will replace the current workspace. Continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Scene: operation error", "The scene operation failed.\n\nDebug detail: simulated failure.", MessageBoxButton.OK, MessageBoxImage.Warning),
        new("Roster: remove jobs", "Permanently remove the selected jobs?", MessageBoxButton.YesNo, MessageBoxImage.Warning),
        new("Log: operation error", "The log operation failed.\n\nDebug detail: simulated failure.", MessageBoxButton.OK, MessageBoxImage.Warning)
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
        pRoot.Children.Add(pWindows);

        pRoot.Children.Add(PSDebugHeadingBuild("Message boxes"));
        var pMessages = PSDebugWrapBuild();
        foreach (PSDebugMessage pMessage in PSDebugMessages)
        {
            PSDebugButtonAdd(pMessages, pMessage.PSDebugName, () => MessageBox.Show(
                this,
                pMessage.PSDebugMessageText,
                pMessage.PSDebugName,
                pMessage.PSDebugButtons,
                pMessage.PSDebugImage));
        }
        pRoot.Children.Add(pMessages);

        return new ScrollViewer
        {
            Content = pRoot,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
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
