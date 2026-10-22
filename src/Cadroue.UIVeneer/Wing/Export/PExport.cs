using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed partial class PExport : UserControl
{
    private readonly QExportCommands _qExportCommands;
    private readonly QExportPreset qExportPreset;

    public QExport QExport { get; }

    public PExport(QExport lExport, LPresetSelection lPresetOwner) :
        this(lExport, null, null, null, null, null)
    {
    }

    internal PExport(
        QExport lExport,
        RoutedEventHandler? creation,
        RoutedEventHandler? deletion,
        RoutedEventHandler? configuration,
        RoutedEventHandler? outbound,
        RoutedEventHandler? inbound)
    {
        QExport = lExport;
        InitializeComponent();
        QExport.QExportSummary.QExportSummaryAttach(PExportDigest);
        _qExportCommands = new QExportCommands(
            QExport,
            () => new PSEncoder(),
            PSVerdict.PSVerdictShow,
            PSWarning.PSWarningShow,
            PExportTheme.PExportTextBrush);
        qExportPreset = new QExportPreset(
            QExport,
            _qExportCommands,
            PExportTheme.PExportMutedBrush,
            () => new PExportPresetGroup(),
            () => new PExportRow(),
            () => new PExportPreset());
        _qExportCommands.QExportCommandsAttach(
            this,
            PExportTitle,
            PExportBody,
            PExportCommands,
            (Border)((Border)Content).Child,
            new PExportCommands(),
            qExportPreset.QExportPresetBuild,
            _qExportCommands.QExportCommandsAttach(),
            _qExportCommands.QExportCommandsAttach(outbound),
            _qExportCommands.QExportCommandsAttach(
                inbound,
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportedName")),
            qExportPreset.QExportPresetAttach,
            qExportPreset.QExportPresetDetach,
            () =>
            {
                qExportPreset.QExportPresetRebuild();
                QExport.QExportSummary.QExportSummaryUpdate();
            },
            creation,
            deletion,
            configuration);
    }
}
