using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QExportPreset(
    QExport lExport,
    QExportCommands qExportCommands,
    Brush pExportMutedBrush,
    Func<Border> pExportPresetGroup,
    Func<ResourceDictionary> pExportRow,
    Func<ResourceDictionary> pExportPreset)
{
    private readonly List<QExportHeader> qExportPresetHeaders = [];
    private readonly List<QExportRow> qExportPresetRows = [];
    private StackPanel qExportPresetPanel = null!;

    public UIElement QExportPresetBuild()
    {
        var scroll = (ScrollViewer)((ControlTemplate)pExportPreset()["PExportPreset"]).LoadContent();
        qExportPresetPanel = (StackPanel)scroll.Content;
        QExportPresetRebuild();
        return scroll;
    }

    public void QExportPresetAttach() =>
        lExport.QExportDrag.QExportDragAttach(qExportPresetPanel, QExportPresetSelect);

    public void QExportPresetRebuild()
    {
        QExportPresetDetach();
        QExportRows rows = lExport.QExportRowsRead();
        IReadOnlyList<QExportEntry> entries = lExport.QExportList.QExportListRead();
        qExportPresetPanel.Children.Clear();
        foreach (QExportEntry entry in entries)
        {
            if (entry.QExportEntryHeader)
            {
                var qExportHeader = new QExportHeader(
                    entry.QExportEntryUser
                        ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Group.User")
                        : entry.QExportEntryGroup!,
                    entry.QExportEntryCollapsed,
                    pExportMutedBrush,
                    () => lExport.QExportList.QExportListToggle(entry, QExportPresetRebuild));
                qExportPresetHeaders.Add(qExportHeader);
                qExportPresetPanel.Children.Add(qExportHeader.QExportHeaderBuild(pExportPresetGroup()));
            }

            var commandHandlers = qExportCommands.QExportCommandsAttach();
            var qExportRow = new QExportRow(
                lExport,
                qExportPresetPanel,
                entry.QExportEntryName,
                rows.QExportRowsRead(entry.QExportEntryName),
                () => lExport.QExportDrag.QExportDragClear(),
                value => lExport.QExportDrag.QExportDragSet(value!),
                lExport.QExportDrag.QExportDragSet,
                commandHandlers.Item4,
                commandHandlers.Item5,
                QExportPresetSelect);
            qExportPresetRows.Add(qExportRow);
            Border root = qExportRow.QExportRowBuild(pExportRow());
            root.Visibility = entry.QExportEntryCollapsed ? Visibility.Collapsed : Visibility.Visible;
            qExportPresetPanel.Children.Add(root);
        }
    }

    public void QExportPresetDetach()
    {
        qExportPresetHeaders.ForEach(header => header.QExportHeaderDetach());
        qExportPresetHeaders.Clear();
        qExportPresetRows.ForEach(row => row.QExportRowDetach());
        qExportPresetRows.Clear();
    }

    private void QExportPresetSelect(string lPresetName)
    {
        lExport.QExportList.QExportListShow(lPresetName, QExportPresetRebuild);
        lExport.QExportSelect(lPresetName);
    }
}
