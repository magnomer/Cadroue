using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PLogFolder
{
    private readonly QLog lLog;

    internal PLogFolder(QLog lLogOwner)
    {
        lLog = lLogOwner;
        PLogFolderCombo = PLogComboBuild(320);
    }

    internal ComboBox PLogFolderCombo { get; }

    internal void PLogFolderAttach()
    {
        lLog.QLogFolder.QLogFilesChanged += PLogFilesApply;
        PLogFolderCombo.SelectionChanged += PLogFileHandle;
        PLogFolderCombo.DropDownOpened += PLogFilesHandle;
    }

    internal void PLogFolderDetach()
    {
        PLogFolderCombo.SelectionChanged -= PLogFileHandle;
        PLogFolderCombo.DropDownOpened -= PLogFilesHandle;
    }

    private void PLogFileHandle(object sender, SelectionChangedEventArgs e) =>
        lLog.QLogFileSelect(PLogFolderCombo.SelectedIndex);

    private void PLogFilesHandle(object? sender, EventArgs e) => lLog.QLogFilesUpdate();

    private void PLogFilesApply()
    {
        PLogFolderCombo.Items.Clear();
        lLog.QLogFolder.QLogFiles.ToList().ForEach(PLogItemAdd);
        PLogFolderCombo.SelectedIndex = lLog.QLogFolder.QLogFileIndex;
    }

    private void PLogItemAdd(QLogFile lFile) =>
        PLogFolderCombo.Items.Add(new ComboBoxItem { Content = lFile.QLogFileLabel, Tag = lFile.QLogFilePath });

    private static ComboBox PLogComboBuild(double pLogWidth)
    {
        var pLogCombo = new ComboBox
        {
            Width = pLogWidth,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        QDropdown.QDropdownAttach(pLogCombo);
        return pLogCombo;
    }
}
