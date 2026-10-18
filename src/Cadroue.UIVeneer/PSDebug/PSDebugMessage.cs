using System.Windows;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed record PSDebugMessage(
    string PSDebugName,
    string PSDebugMessageText,
    PSDebugMessage.PSDebugKind PSDebugMessageKind)
{
    internal enum PSDebugKind
    {
        PSDebugAnnouncement,
        PSDebugWarning,
        PSDebugAlert,
        PSDebugDecision,
        PSDebugChoice
    }

    internal static readonly PSDebugMessage[] PSDebugMessages =
    {
        new(
            "About: resource open failure",
            "The requested link or resource could not be opened.\n\nDebug detail: simulated failure.",
            PSDebugKind.PSDebugWarning),
        new(
            "Options: settings save failure",
            "Settings could not be saved.\n\nDebug detail: simulated failure.",
            PSDebugKind.PSDebugWarning),
        new(
            "Options: language restart required",
            "Restart Cadroue to finish changing the language.",
            PSDebugKind.PSDebugAnnouncement),
        new(
            "System: Flyleaf installation result",
            "Flyleaf installation finished. This is a simulated result.",
            PSDebugKind.PSDebugAnnouncement),
        new("System: no file records", "There are no file records to remove.", PSDebugKind.PSDebugAnnouncement),
        new("System: clear file records", "Remove all saved file records?", PSDebugKind.PSDebugAlert),
        new("System: file records removed", "3 file records were removed.", PSDebugKind.PSDebugAnnouncement),
        new("System: clear completed work", "Remove all completed work records?", PSDebugKind.PSDebugAlert),
        new(
            "System: completed work removed",
            "3 completed work records were removed.",
            PSDebugKind.PSDebugAnnouncement),
        new(
            "System: workspace running",
            "The workspace cannot be reset while work is running.",
            PSDebugKind.PSDebugWarning),
        new("System: reset workspace", "Reset the entire workspace?", PSDebugKind.PSDebugAlert),
        new("System: workspace reset", "The workspace was reset.", PSDebugKind.PSDebugAnnouncement),
        new(
            "System: MPV installation result",
            "MPV installation finished. This is a simulated result.",
            PSDebugKind.PSDebugAnnouncement),
        new("Timeline: invalid palette", "The selected palette is invalid.", PSDebugKind.PSDebugWarning),
        new("Timeline: remove palette", "Remove the selected palette?", PSDebugKind.PSDebugAlert),
        new(
            "LosslessCut: detected project",
            "A LosslessCut project was detected. Use it for this import?",
            PSDebugKind.PSDebugDecision),
        new("LosslessCut: missing media", "The project media could not be found.", PSDebugKind.PSDebugAnnouncement),
        new("LosslessCut: read failure", "The LosslessCut project could not be read.", PSDebugKind.PSDebugWarning),
        new(
            "LosslessCut: version warning",
            "This project was created by a different version. Continue?",
            PSDebugKind.PSDebugDecision),
        new(
            "LosslessCut: media mismatch",
            "The selected media does not match the project. Continue?",
            PSDebugKind.PSDebugDecision),
        new(
            "LosslessCut: empty project",
            "The project contains no segments to import.",
            PSDebugKind.PSDebugAnnouncement),
        new("LosslessCut: import mode", "Import segments as one tab or as separate tabs?", PSDebugKind.PSDebugChoice),
        new("Preset: write failure", "The export preset could not be written.", PSDebugKind.PSDebugWarning),
        new("Preset: read failure", "The export preset could not be read.", PSDebugKind.PSDebugWarning),
        new("Preset: invalid import", "The imported export preset is invalid.", PSDebugKind.PSDebugWarning),
        new("Preset: missing preset", "The selected export preset no longer exists.", PSDebugKind.PSDebugAnnouncement),
        new(
            "Source: unsupported audio",
            "This audio-only file is not supported by this source field.",
            PSDebugKind.PSDebugAnnouncement),
        new("Sidecar: read failure", "The sidecar file could not be read.", PSDebugKind.PSDebugWarning),
        new(
            "Sidecar: source mismatch (load)",
            "The sidecar belongs to another source. Load it anyway?",
            PSDebugKind.PSDebugDecision),
        new(
            "Sidecar: source mismatch (save)",
            "The existing sidecar belongs to another source. Replace it?",
            PSDebugKind.PSDebugAlert),
        new(
            "Tabs: busy worklist",
            "This tab cannot be moved while its worklist is busy.",
            PSDebugKind.PSDebugAnnouncement),
        new("Schedule: cancel running jobs", "Cancel the running jobs?", PSDebugKind.PSDebugAlert),
        new("Schedule: destructive clear", "Permanently clear the selected jobs or tabs?", PSDebugKind.PSDebugAlert),
        new("Scene: missing name", "Enter a scene name before saving.", PSDebugKind.PSDebugAnnouncement),
        new(
            "Scene: load confirmation",
            "Loading this scene will replace the current workspace. Continue?",
            PSDebugKind.PSDebugAlert),
        new(
            "Scene: operation error",
            "The scene operation failed.\n\nDebug detail: simulated failure.",
            PSDebugKind.PSDebugWarning),
        new("Roster: remove jobs", "Permanently remove the selected jobs?", PSDebugKind.PSDebugAlert),
        new(
            "Log: operation error",
            "The log operation failed.\n\nDebug detail: simulated failure.",
            PSDebugKind.PSDebugWarning)
    };

    internal void PSDebugMessageShow(Window pOwner)
    {
        switch (PSDebugMessageKind)
        {
            case PSDebugKind.PSDebugWarning:
                PSWarning.PSWarningShow(pOwner, PSDebugName, PSDebugMessageText);
                break;
            case PSDebugKind.PSDebugAlert:
                PSAlert.PSAlertConfirm(
                    pOwner,
                    PSDebugName,
                    PSDebugMessageText,
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Delete"));
                break;
            case PSDebugKind.PSDebugDecision:
                PSDecision.PSDecisionConfirm(
                    pOwner,
                    PSDebugName,
                    PSDebugMessageText,
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Continue"),
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel"));
                break;
            case PSDebugKind.PSDebugChoice:
                PSDecision.PSDecisionSelect(
                    pOwner,
                    PSDebugName,
                    PSDebugMessageText,
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Replace"),
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Append"),
                    QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel"));
                break;
            default:
                PSAnnouncement.PSAnnouncementShow(pOwner, PSDebugName, PSDebugMessageText);
                break;
        }
    }
}
