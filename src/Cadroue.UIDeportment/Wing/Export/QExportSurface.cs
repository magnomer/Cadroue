using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QExportSurface
{
    public static QExportSurfaceType QExportSurfaceBuild<QExportSurfaceType>(
        QExport export,
        QExportSurfaceType surface,
        Func<Window> encoderCreate,
        Action<Window, QSVerdict> verdict,
        Action<Window?, string, string> warning,
        Func<Border> presetGroupCreate,
        Func<ResourceDictionary> rowCreate,
        Func<ResourceDictionary> presetCreate,
        Func<ResourceDictionary> commandsCreate,
        RoutedEventHandler? creation = null,
        RoutedEventHandler? deletion = null,
        RoutedEventHandler? configuration = null,
        RoutedEventHandler? outbound = null,
        RoutedEventHandler? inbound = null)
        where QExportSurfaceType : UserControl
    {
        Border summary = QContract.QContractFind<Border>(surface, "PExportDigest");
        export.QExportSummary.QExportSummaryAttach(summary);

        var commands = new QExportCommands(
            export,
            encoderCreate,
            verdict,
            warning,
            QContract.QContractSheetFind<Brush>(surface, "PExportTextBrush"));
        var preset = new QExportPreset(
            export,
            commands,
            QContract.QContractSheetFind<Brush>(surface, "PExportMutedBrush"),
            presetGroupCreate,
            rowCreate,
            presetCreate);
        commands.QExportCommandsAttach(
            surface,
            QContract.QContractFind<TextBlock>(surface, "PExportTitle"),
            QContract.QContractFind<ContentControl>(surface, "PExportBody"),
            QContract.QContractFind<ContentControl>(surface, "PExportCommands"),
            QContract.QContractFind<Border>(surface, "PExportFrame"),
            commandsCreate(),
            preset.QExportPresetBuild,
            commands.QExportCommandsAttach(),
            commands.QExportCommandsAttach(outbound),
            commands.QExportCommandsAttach(
                inbound,
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportedName")),
            preset.QExportPresetAttach,
            preset.QExportPresetDetach,
            () =>
            {
                preset.QExportPresetRebuild();
                export.QExportSummary.QExportSummaryUpdate();
            },
            creation,
            deletion,
            configuration);
        return surface;
    }
}
