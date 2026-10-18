using System.Windows.Controls;

namespace Cadroue.UIVeneer;

internal static class PSOptionsFolder
{
    internal const string PSOptionsBrowseIcon = "/PAsset/PPanel/PBrowse.svg";
    internal const string PSOptionsOpenIcon = "/PAsset/PPanel/POpen.svg";

    internal static void PSSystemFolderRead(TextBox pPathBox, string pDialogTitle, string pFallback)
    {
        string pInitial = PSSystemFolderResolve(pPathBox.Text, pFallback);
        var pDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = pDialogTitle,
            InitialDirectory = pInitial
        };
        if (pDialog.ShowDialog() == true)
        {
            PSSystemPathShow(pPathBox, pDialog.FolderName);
        }
    }

    private static string PSSystemFolderResolve(string pPath, string pFallback) =>
        string.IsNullOrWhiteSpace(pPath) ? pFallback : pPath;

    private static void PSSystemPathShow(TextBox pPathBox, string pFolder) => pPathBox.Text = pFolder;
}
