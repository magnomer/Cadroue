using Cadroue.Conduct;
using Cadroue.Infrastructure;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QSFlyleaf : CSFlyleafPort
{
    private readonly CSFlyleaf csFlyleaf;
    private readonly RoutedEventHandler qsFlyleafInstaller;
    private readonly RoutedEventHandler qsFlyleafOpener;
    private Button? psFlyleafInstaller;
    private Button? psFlyleafOpener;
    private QSFlyleafAnswer? qsFlyleafAnswer;
    private QSOptionsOperation? qsOptionsOperation;
    private (Action<bool>, Action<bool, string>) qsFlyleafOutcome;

    public QSFlyleaf()
    {
        csFlyleaf = new CSFlyleaf(this);
        qsFlyleafInstaller = QSFlyleafStart;
        qsFlyleafOpener = QSFlyleafOpen;
    }

    public QSFlyleaf(CSFlyleafPort port)
    {
        csFlyleaf = new CSFlyleaf(port);
        qsFlyleafInstaller = QSFlyleafStart;
        qsFlyleafOpener = QSFlyleafOpen;
    }

    public FrameworkElement QSFlyleafAttach(FrameworkElement surface, QSFlyleafAnswer answer)
    {
        QSFlyleafDetach();
        psFlyleafInstaller = QContract.QContractFind<Button>(surface, "PSFlyleafInstaller");
        psFlyleafOpener = QContract.QContractFind<Button>(surface, "PSFlyleafOpener");
        ProgressBar psFlyleafProgress = QContract.QContractFind<ProgressBar>(surface, "PSFlyleafProgress");
        qsFlyleafAnswer = answer;
        QSOptionsOperation operation = new(psFlyleafInstaller, psFlyleafProgress);
        qsOptionsOperation = operation;
        qsFlyleafOutcome = (
            installed => QSFlyleafUpdate(operation, installed, () => answer.QSFlyleafShow(
                true,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf"),
                QLocalizationCatalog.QLocalizationTextRead("Flyleaf.Local.Install.Completed"))),
            (installed, failure) => QSFlyleafUpdate(operation, installed, () => answer.QSFlyleafShow(
                false,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf"),
                QLocalizationCatalog.QLocalizationFormat("Flyleaf.Local.Install.Failed", failure))));

        QButton.QButtonAttach(psFlyleafInstaller, QButtonLook.QButtonLookWhite);
        QButton.QButtonAttach(psFlyleafOpener, QButtonLook.QButtonLookWhite);
        Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
        icon.Freeze();
        psFlyleafOpener.Content = new Image
        {
            Source = QIcon.QIconRead("/PAsset/PPanel/POpen.svg", icon),
            Width = 22,
            Height = 22,
            Stretch = Stretch.Uniform
        };
        psFlyleafOpener.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.System.Open");
        QSFlyleafUpdate(psFlyleafInstaller, csFlyleaf.CSFlyleafInstalledCheck());
        psFlyleafInstaller.Click += qsFlyleafInstaller;
        psFlyleafOpener.Click += qsFlyleafOpener;
        return surface;
    }

    public void QSFlyleafDetach()
    {
        if (psFlyleafInstaller is not null)
        {
            psFlyleafInstaller.Click -= qsFlyleafInstaller;
            psFlyleafOpener!.Click -= qsFlyleafOpener;
        }

        qsOptionsOperation = null;
        qsFlyleafOutcome = default;
        psFlyleafInstaller = null;
        psFlyleafOpener = null;
        qsFlyleafAnswer = null;
    }

    public async Task QSFlyleafStart()
    {
        QSOptionsOperation operation = qsOptionsOperation!;
        if (operation is null)
        {
            throw new NullReferenceException();
        }
        IProgress<double> progress = new Progress<double>(value =>
            QSFlyleafOperationCheck(operation)?.QSOptionsOperationApply(value));
        try
        {
            await csFlyleaf.CSFlyleafStart(
                progress,
                operation.QSOptionsOperationStart,
                qsFlyleafOutcome.Item1,
                qsFlyleafOutcome.Item2);
        }
        catch (Exception exception)
        {
            QSFlyleafOperationCheck(operation)?.QSOptionsOperationHide();
            if (QSFlyleafOperationCheck(operation) is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }
    }

    private void QSFlyleafUpdate(QSOptionsOperation operation, bool installed, Action show)
    {
        QSFlyleafOperationCheck(operation)?.QSOptionsOperationHide();
        QSFlyleafOperationCheck(operation)?.QSOptionsOperationApply(installer =>
        {
            QSFlyleafUpdate(installer, installed);
            installer.IsEnabled = true;
            show();
        });
    }

    public void QSFlyleafOpen() => qsFlyleafAnswer!.QSFlyleafOpen();

    private async void QSFlyleafStart(object sender, RoutedEventArgs _) => await QSFlyleafStart();

    private void QSFlyleafOpen(object sender, RoutedEventArgs _) => QSFlyleafOpen();

    private QSOptionsOperation? QSFlyleafOperationCheck(QSOptionsOperation operation) =>
        ReferenceEquals(qsOptionsOperation, operation) ? operation : null;

    private static void QSFlyleafUpdate(Button installer, bool installed)
    {
        installer.Content = QLocalizationCatalog.QLocalizationTextRead(
            installed
                ? "Options.System.ReinstallFlyleaf"
                : "Options.System.InstallFlyleaf");
    }

    bool CSFlyleafPort.CSFlyleafInstalledCheck() => LFlyleafFolder.LFlyleafInstalledCheck();

    async Task<(bool, string, bool)> CSFlyleafPort.CSFlyleafStart(IProgress<double> progress)
    {
        LFlyleafInstallResult result = await LFlyleafInstall.LFlyleafInstallStart(progress);
        return (
            result.LFlyleafInstallSuccess,
            result.LFlyleafInstallMessage,
            LFlyleafFolder.LFlyleafInstalledCheck());
    }
}
