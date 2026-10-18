using Cadroue.UIDeportment;
using Microsoft.Win32;

namespace Cadroue.UIVeneer;

public sealed class PListDialog
{
    private readonly QList lList;

    public PListDialog(QList lListOwner) => lList = lListOwner;

    public void PListFilesOpen()
    {
        var pDialog = new OpenFileDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.AddFiles"),
            Multiselect = true,
            Filter = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.MediaFilter")
        };
        lList.QListDialogAdd(pDialog.ShowDialog(), pDialog.FileNames, "file");
    }

    public void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.AddFolder"),
            Multiselect = true
        };
        lList.QListDialogAdd(pDialog.ShowDialog(), pDialog.FolderNames, "folder");
    }
}
