using Cadroue.Conduct;
using Cadroue.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed record QSRendererSnapshot(
    string QSRendererSnapshotPath,
    string QSRendererSnapshotStatus);

public sealed class QSRenderer : CSRendererPort
{
    private static readonly IReadOnlyDictionary<CSRendererState, string> qsRendererStates =
        new Dictionary<CSRendererState, string>
        {
            [CSRendererState.CSRendererStateBlank] = "Options.System.FFmpegBlank",
            [CSRendererState.CSRendererStateReady] = "Options.System.FFmpegReady",
            [CSRendererState.CSRendererStateProgram] = "Options.System.FFmpegProgramOnly",
            [CSRendererState.CSRendererStateLibrary] = "Options.System.FFmpegLibraryOnly",
            [CSRendererState.CSRendererStateMissing] = "Options.System.FFmpegMissing"
        };

    private readonly CSRenderer csRenderer;
    private QSRendererSnapshot qsRendererSnapshot;
    private string? qsRendererPath;
    private TextBox? psRendererPath;
    private TextBlock? psRendererStatus;
    private Button? psRendererFinder;
    private Button? psRendererOpener;
    private Button? psRendererExaminer;
    private QSRendererAnswer? qsRendererAnswer;
    private readonly Delegate[] qsRendererHandlers;

    public QSRenderer(string path)
    {
        csRenderer = new CSRenderer(this);
        qsRendererHandlers =
        [
            new TextChangedEventHandler(QSRendererUpdate),
            new RoutedEventHandler(QSRendererUpdate),
            new RoutedEventHandler(QSRendererOpen),
            new RoutedEventHandler(QSRendererDiagnosisShow)
        ];
        qsRendererSnapshot = new QSRendererSnapshot(
            path,
            qsRendererStates[CSRendererState.CSRendererStateBlank]);
    }

    public QSRenderer(string path, CSRendererPort port)
    {
        csRenderer = new CSRenderer(port);
        qsRendererHandlers =
        [
            new TextChangedEventHandler(QSRendererUpdate),
            new RoutedEventHandler(QSRendererUpdate),
            new RoutedEventHandler(QSRendererOpen),
            new RoutedEventHandler(QSRendererDiagnosisShow)
        ];
        qsRendererSnapshot = new QSRendererSnapshot(
            path,
            qsRendererStates[CSRendererState.CSRendererStateBlank]);
    }

    public void QSRendererAttach(FrameworkElement surface, QSRendererAnswer answer)
    {
        QSRendererDetach();
        var root = (StackPanel)((ContentControl)surface).Content;
        var row = (Grid)root.Children[1];
        ((TextBlock)root.Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpeg");
        ((TextBlock)row.Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Location");

        psRendererPath = QContract.QContractFind<TextBox>(surface, "PSRendererPath");
        psRendererStatus = QContract.QContractFind<TextBlock>(surface, "PSRendererStatus");
        psRendererFinder = QContract.QContractFind<Button>(surface, "PSRendererFinder");
        psRendererOpener = QContract.QContractFind<Button>(surface, "PSRendererOpener");
        psRendererExaminer = QContract.QContractFind<Button>(surface, "PSRendererExaminer");
        qsRendererAnswer = answer;

        QTextbox.QTextboxAttach(psRendererPath);
        psRendererPath.Padding = new Thickness(6, 0, 10, 0);
        Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
        icon.Freeze();
        QSRendererUpdate(psRendererFinder, "/PAsset/PPanel/PBrowse.svg", "Options.System.Browse", icon);
        QSRendererUpdate(psRendererOpener, "/PAsset/PPanel/POpen.svg", "Options.System.Open", icon);
        QSRendererUpdate(psRendererExaminer, "/PAsset/PPanel/PDiagnosis.svg", "Options.System.Diagnosis", icon);
        psRendererPath.Text = qsRendererPath ?? qsRendererSnapshot.QSRendererSnapshotPath;

        psRendererPath.TextChanged += (TextChangedEventHandler)qsRendererHandlers[0];
        psRendererFinder.Click += (RoutedEventHandler)qsRendererHandlers[1];
        psRendererOpener.Click += (RoutedEventHandler)qsRendererHandlers[2];
        psRendererExaminer.Click += (RoutedEventHandler)qsRendererHandlers[3];
        QSRendererUpdate(psRendererPath.Text);
    }

    public void QSRendererDetach()
    {
        if (psRendererPath is not null)
        {
            psRendererPath!.TextChanged -= (TextChangedEventHandler)qsRendererHandlers[0];
            psRendererFinder!.Click -= (RoutedEventHandler)qsRendererHandlers[1];
            psRendererOpener!.Click -= (RoutedEventHandler)qsRendererHandlers[2];
            psRendererExaminer!.Click -= (RoutedEventHandler)qsRendererHandlers[3];
        }

        qsRendererAnswer = null;
        psRendererPath = null;
        psRendererStatus = null;
        psRendererFinder = null;
        psRendererOpener = null;
        psRendererExaminer = null;
    }

    public void QSRendererUpdate(string path)
    {
        qsRendererPath = path;
        qsRendererSnapshot = QSRendererUpdate(csRenderer.CSRendererRead(path));
        if (psRendererStatus is not null)
        {
            psRendererStatus.Text = QLocalizationCatalog.QLocalizationTextRead(
                qsRendererSnapshot.QSRendererSnapshotStatus);
        }
    }

    private void QSRendererUpdate(object sender, TextChangedEventArgs _) =>
        QSRendererUpdate(((TextBox)sender).Text);

    private void QSRendererUpdate(object sender, RoutedEventArgs _)
    {
        string? folder = qsRendererAnswer!.QSRendererFolderRead(
            qsRendererPath ?? qsRendererSnapshot.QSRendererSnapshotPath);
        if (folder is not null)
        {
            psRendererPath!.Text = folder;
        }
    }

    private static QSRendererSnapshot QSRendererUpdate(CSRendererSnapshot snapshot) =>
        new(
            snapshot.CSRendererSnapshotPath,
            qsRendererStates[snapshot.CSRendererSnapshotState]);

    private static void QSRendererUpdate(Button button, string path, string tooltip, Brush icon)
    {
        button.Content = new Image
        {
            Source = QIcon.QIconRead(path, icon),
            Width = 22,
            Height = 22,
            Stretch = Stretch.Uniform
        };
        button.ToolTip = QLocalizationCatalog.QLocalizationTextRead(tooltip);
        QButton.QButtonAttach(button, QButtonLook.QButtonLookWhite);
    }

    public void QSRendererOpen() =>
        qsRendererAnswer!.QSRendererPathOpen(qsRendererPath ?? qsRendererSnapshot.QSRendererSnapshotPath);

    private void QSRendererOpen(object sender, RoutedEventArgs _) => QSRendererOpen();

    public void QSRendererDiagnosisShow() => qsRendererAnswer!.QSRendererDiagnosisShow();

    private void QSRendererDiagnosisShow(object sender, RoutedEventArgs _) => QSRendererDiagnosisShow();

    public string QSRendererApply() => qsRendererPath ?? qsRendererSnapshot.QSRendererSnapshotPath;

    bool CSRendererPort.CSRendererProgramCheck(string path) => LRendererLibrary.LRendererProgramExist(path);

    bool CSRendererPort.CSRendererLibraryCheck(string path) => LRendererLibrary.LRendererFolderValidate(path);
}
