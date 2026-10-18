using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Microsoft.Win32;

namespace Cadroue.UIDeportment;

public sealed class QSource
{
    private const string QSourceSidecarPattern = "*.cad";
    private const string QSourceReturnKey = "Return";

    private readonly CUsher qSourceUsher;
    private readonly QViewer qSourceViewer;
    private readonly bool qSourceAudioAllowed;
    private string qSourcePath = string.Empty;
    private UserControl qSourceSurface = null!;

    public event Action<string>? QSourcePathApplied;
    public event Action<string, string>? QSourceRefuse;

    public QSource(CUsher lUsher, QViewer lViewer, bool lAudioAllowed)
    {
        qSourceUsher = lUsher;
        qSourceViewer = lViewer;
        qSourceAudioAllowed = lAudioAllowed;
        qSourceViewer.QViewerMediaChanged += QSourceMediaHandle;
    }

    private TextBox QSourcePathBox => QContract.QContractFind<TextBox>(qSourceSurface, "pSourcePath");

    private TextBlock QSourcePlaceholder => QContract.QContractFind<TextBlock>(qSourceSurface, "pSourcePlaceholder");

    private Button QSourceBrowse => QContract.QContractFind<Button>(qSourceSurface, "pSourceBrowse");

    // Subscriptions sit before the placeholder writes; those writes raise none of the subscribed events.
    public UserControl QSourceAttach(UserControl lSurface)
    {
        QSourceSurfaceSet(lSurface);
        QButton.QButtonAttach(QSourceBrowse, QButtonLook.QButtonLookSource);
        QSourceHandlersAttach();
        QSourcePlaceholderApply(QSourcePlaceholderRead());
        QSourcePlaceholderSync();
        return qSourceSurface;
    }

    private void QSourceSurfaceSet(UserControl lSurface) => qSourceSurface = lSurface;

    private void QSourceHandlersAttach()
    {
        QSourcePathBox.KeyDown += QSourceKeyHandle;
        QSourcePathBox.TextChanged += QSourceTextHandle;
        QSourceBrowse.Click += QSourceBrowseHandle;
        QSourcePathApplied += QSourcePathApply;
        QSourceRefuse += QWarningNotice.QWarningPublish;
    }

    private void QSourcePlaceholderApply(string lText) => QSourcePlaceholder.Text = lText;

    public string QSourcePath => qSourcePath;

    public bool QSourceAudioAllowed => qSourceAudioAllowed;

    public static string QSourcePlaceholderRead() => QLocalizationCatalog.QLocalizationTextRead("Source.Empty.Notice");

    public static string QSourceTitleRead() => QLocalizationCatalog.QLocalizationTextRead("Source.Dialog.Open");

    public static bool QSourcePlaceholderCheck(string lText) => string.IsNullOrWhiteSpace(lText);

    public string QSourceFilterRead()
    {
        string lVideoPattern = QSourcePatternRead(LMedia.LMediaVideoExtensions);
        string lAudioPattern = QSourcePatternRead(LMedia.LMediaAudioExtensions);
        return qSourceAudioAllowed
            ? QLocalizationCatalog.QLocalizationFormat(
                "Source.Dialog.MediaProjectFilter", lVideoPattern, lAudioPattern, QSourceSidecarPattern)
            : QLocalizationCatalog.QLocalizationFormat(
                "Source.Dialog.VideoProjectFilter", lVideoPattern, QSourceSidecarPattern);
    }

    public void QSourceDialogOpen(bool? lConfirmed, string lPath)
    {
        if (lConfirmed != true)
        {
            return;
        }

        QSourceOpen(lPath);
    }

    public bool QSourceKeyRun(string lKey, string lText)
    {
        if (!string.Equals(lKey, QSourceReturnKey, StringComparison.Ordinal))
        {
            return false;
        }

        string lPath = lText.Trim();
        if (!qSourceUsher.CUsherFileExist(lPath))
        {
            return false;
        }

        return QSourceOpen(lPath);
    }

    public bool QSourceOpen(string lPath)
    {
        if (LMedia.LMediaAudioCheck(lPath) && !qSourceAudioAllowed)
        {
            QSourceRefuse?.Invoke(
                QLocalizationCatalog.QLocalizationTextRead("Source.AudioOnly.Title"),
                QLocalizationCatalog.QLocalizationTextRead("Source.AudioOnly.Message"));
            return false;
        }

        qSourceViewer.QViewerSource.QViewerSourceOpen(lPath);
        return true;
    }

    private void QSourcePathApply(string lPath) => QSourcePathBox.Text = lPath;

    private void QSourceTextHandle(object sender, TextChangedEventArgs e) => QSourcePlaceholderSync();

    private void QSourceBrowseHandle(object sender, RoutedEventArgs e)
    {
        OpenFileDialog lDialog = QSourceDialogBuild();
        QSourceDialogOpen(lDialog.ShowDialog(), lDialog.FileName);
    }

    private OpenFileDialog QSourceDialogBuild() =>
        new()
        {
            Title = QSourceTitleRead(),
            Filter = QSourceFilterRead()
        };

    private void QSourceKeyHandle(object sender, KeyEventArgs e) =>
        e.Handled = QSourceKeyRun(e.Key.ToString(), QSourcePathBox.Text);

    private void QSourcePlaceholderSync() =>
        QSourceVisibilityApply(QSourceVisibilityResolve(QSourcePathBox.Text));

    private static Visibility QSourceVisibilityResolve(string lText) =>
        QSourcePlaceholderCheck(lText) ? Visibility.Visible : Visibility.Collapsed;

    private void QSourceVisibilityApply(Visibility lVisibility) => QSourcePlaceholder.Visibility = lVisibility;

    private void QSourceMediaHandle(LCargo lCargo)
    {
        qSourcePath = lCargo.LCargoSourcePath;
        QSourcePathApplied?.Invoke(qSourcePath);
    }

    private static string QSourcePatternRead(IReadOnlyList<string> lExtensions) =>
        string.Join(";", lExtensions.Select(lExtension => $"*{lExtension}"));
}
