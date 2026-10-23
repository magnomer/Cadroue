using Cadroue.Conduct;
using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QSWorkspace
{
    private readonly CSOptions csOptions;
    private readonly Delegate[] qsWorkspace;
    private FrameworkElement? psWorkspace;
    private QSWorkspaceAnswer? qsWorkspaceAnswer;
    private Action[] qsWorkspacePending = [];

    public QSWorkspace(CSOptions options)
    {
        csOptions = options;
        qsWorkspace =
        [
            new TextChangedEventHandler(QSWorkspaceUpdate),
            new RoutedEventHandler(QSWorkspaceFolderRead),
            new RoutedEventHandler((_, args) => QSWorkspaceFolderRead(args)),
            new RoutedEventHandler(QSWorkspaceDoneClear),
            new RoutedEventHandler(QSWorkspaceClear)
        ];
    }

    public FrameworkElement QSWorkspaceAttach(
        FrameworkElement surface,
        string folder,
        QSWorkspaceAnswer answer,
        out Button done)
    {
        QSWorkspaceDetach();
        psWorkspace = surface;
        qsWorkspaceAnswer = answer;
        var path = QContract.QContractFind<TextBox>(surface, "PSWorkspaceBox");
        path.Text = folder;
        qsWorkspacePending = [QSWorkspaceDoneConfirm, QSWorkspaceClearConfirm];
        Button browse = QContract.QContractFind<Button>(surface, "PSWorkspaceBrowse");
        Button open = QContract.QContractFind<Button>(surface, "PSWorkspaceOpener");
        Button clear = QContract.QContractFind<Button>(surface, "PSWorkspaceCleaner");
        done = (Button)surface.FindResource("PSSystemDone");
        QContract.QContractFind<TextBlock>(surface, "PSWorkspaceTitle").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Workspace");
        QContract.QContractFind<TextBlock>(surface, "PSWorkspaceLocation").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Location");
        QContract.QContractFind<TextBlock>(surface, "PSWorkspaceCurrent").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CurrentSize");
        QContract.QContractFind<TextBlock>(surface, "PSWorkspaceMaintenance").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance");
        QContract.QContractFind<TextBlock>(surface, "PSSystemMaintenance").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceUnapplied");
        QContract.QContractFind<TextBlock>(surface, "PSWorkspaceDefault").Text =
            QLocalizationCatalog.QLocalizationFormat(
                "Options.System.DefaultPath",
                ((LOptionsPort)csOptions).CSOptionsDefaultRead(string.Empty));
        browse.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse");
        open.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.System.Open");
        clear.Content = QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspace");
        done.Content = QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDone");
        Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
        icon.Freeze();
        browse.Content = QSWorkspaceUpdate("/PAsset/PPanel/PBrowse.svg", icon);
        open.Content = QSWorkspaceUpdate("/PAsset/PPanel/POpen.svg", icon);
        path.TextChanged += (TextChangedEventHandler)qsWorkspace[0];
        browse.Click += (RoutedEventHandler)qsWorkspace[1];
        open.Click += (RoutedEventHandler)qsWorkspace[2];
        done.Click += (RoutedEventHandler)qsWorkspace[3];
        clear.Click += (RoutedEventHandler)qsWorkspace[4];
        QSWorkspaceSizeUpdate();
        QSWorkspaceUpdate();
        return surface;
    }

    public void QSWorkspaceDetach()
    {
        if (psWorkspace is null)
        {
            return;
        }
        QContract.QContractFind<TextBox>(psWorkspace, "PSWorkspaceBox").TextChanged -=
            (TextChangedEventHandler)qsWorkspace[0];
        QContract.QContractFind<Button>(psWorkspace, "PSWorkspaceBrowse").Click -=
            (RoutedEventHandler)qsWorkspace[1];
        QContract.QContractFind<Button>(psWorkspace, "PSWorkspaceOpener").Click -=
            (RoutedEventHandler)qsWorkspace[2];
        ((Button)psWorkspace.FindResource("PSSystemDone")).Click -= (RoutedEventHandler)qsWorkspace[3];
        QContract.QContractFind<Button>(psWorkspace, "PSWorkspaceCleaner").Click -=
            (RoutedEventHandler)qsWorkspace[4];
        qsWorkspacePending[0] = qsWorkspacePending[1] = static () => { };
        psWorkspace = null;
        qsWorkspaceAnswer = null;
    }

    public void QSWorkspaceUpdate() =>
        ((LOptionsPort)csOptions).CSOptionsAppliedCheck(
            QSWorkspaceFolderRead(),
            _ => QSWorkspaceUpdate(true),
            () => QSWorkspaceUpdate(false));

    public void QSWorkspaceSizeUpdate()
    {
        long size = ((LOptionsPort)csOptions).CSOptionsSizeRead();
        QContract.QContractFind<TextBlock>(psWorkspace!, "PSWorkspaceSize").Text = QSWorkspaceUpdate(size);
    }

    public void QSWorkspaceCommit() =>
        ((LOptionsPort)csOptions).CSOptionsWorkspaceCommit(QSWorkspaceFolderRead(), QSWorkspaceCommit);

    private void QSWorkspaceCommit(string kept, string destination)
    {
        QContract.QContractFind<TextBox>(psWorkspace!, "PSWorkspaceBox").Text = kept;
        qsWorkspaceAnswer!.QSWorkspaceWarningShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Workspace"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.WorkspaceOccupied", destination));
    }

    private void QSWorkspaceUpdate(object sender, TextChangedEventArgs _) => QSWorkspaceUpdate();

    private void QSWorkspaceUpdate(bool applied)
    {
        ((Button)psWorkspace!.FindResource("PSSystemDone")).IsEnabled = applied;
        QContract.QContractFind<Button>(psWorkspace, "PSWorkspaceCleaner").IsEnabled = applied;
        QContract.QContractFind<UIElement>(psWorkspace, "PSSystemMaintenance").Visibility =
            applied ? Visibility.Collapsed : Visibility.Visible;
        UIElement notice = QContract.QContractFind<UIElement>(psWorkspace, "PSWorkspaceDefault");
        ((LOptionsPort)csOptions).CSOptionsDefaultCheck(
            QSWorkspaceFolderRead(),
            () => notice.Visibility = Visibility.Visible,
            () => notice.Visibility = Visibility.Collapsed);
    }

    private static Image QSWorkspaceUpdate(string path, Brush icon) => new()
    {
        Source = QIcon.QIconRead(path, icon),
        Width = 22,
        Height = 22,
        Stretch = Stretch.Uniform
    };

    private static string QSWorkspaceUpdate(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} {units[0]}" : $"{value:0.##} {units[unit]}";
    }

    private void QSWorkspaceFolderRead(object sender, RoutedEventArgs _)
    {
        QSWorkspaceAnswer answer = qsWorkspaceAnswer!;
        var path = QContract.QContractFind<TextBox>(psWorkspace!, "PSWorkspaceBox");
        answer.QSWorkspaceFolderShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseWorkspace"),
            ((LOptionsPort)csOptions).CSOptionsDefaultRead(QSWorkspaceFolderRead()),
            folder => QSWorkspaceFolderRead(answer, path, folder));
    }

    private void QSWorkspaceFolderRead(QSWorkspaceAnswer answer, TextBox path, string folder)
    {
        if (ReferenceEquals(answer, qsWorkspaceAnswer))
        {
            path.Text = folder;
        }
    }

    private void QSWorkspaceFolderRead(RoutedEventArgs _) =>
        ((LOptionsPort)csOptions).CSOptionsFolderOpen(QSWorkspaceFolderRead());

    private string QSWorkspaceFolderRead() => QContract.QContractFind<TextBox>(psWorkspace!, "PSWorkspaceBox").Text;

    private void QSWorkspaceDoneClear(object sender, RoutedEventArgs _) =>
        ((LOptionsPort)csOptions).CSOptionsAppliedCheck(QSWorkspaceFolderRead(), QSWorkspaceDoneClear, () => { });

    private void QSWorkspaceDoneClear(string root)
    {
        Action[] pending = qsWorkspacePending;
        qsWorkspaceAnswer!.QSWorkspaceConfirmationShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.ClearDoneConfirm", root),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Delete"),
            () => pending[0]());
    }

    private void QSWorkspaceDoneConfirm()
    {
        LOptionsFact result = ((LOptionsPort)csOptions).CSOptionsDoneClear();
        QContract.QContractFind<TextBlock>(psWorkspace!, "PSWorkspaceSize").Text =
            QSWorkspaceUpdate(result.LOptionsFactSize);
        qsWorkspaceAnswer!.QSWorkspaceAnnouncementShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.WorkRecordsRemoved", result.LOptionsFactCount));
    }

    private void QSWorkspaceClear(object sender, RoutedEventArgs _) =>
        ((LOptionsPort)csOptions).CSOptionsRunningCheck(QSWorkspaceFolderRead(), QSWorkspaceClear, QSWorkspaceClear);

    private void QSWorkspaceClear(string root)
    {
        Action[] pending = qsWorkspacePending;
        qsWorkspaceAnswer!.QSWorkspaceConfirmationShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.ClearWorkspaceConfirm", root),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Reset"),
            () => pending[1]());
    }

    private void QSWorkspaceClear() =>
        qsWorkspaceAnswer!.QSWorkspaceWarningShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceRunning"));

    private void QSWorkspaceClearConfirm()
    {
        LOptionsFact result = ((LOptionsPort)csOptions).CSOptionsWorkspaceClear();
        QContract.QContractFind<TextBlock>(psWorkspace!, "PSWorkspaceSize").Text =
            QSWorkspaceUpdate(result.LOptionsFactSize);
        qsWorkspaceAnswer!.QSWorkspaceAnnouncementShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceReset"));
    }
}
