using Cadroue.Conduct;
using Cadroue.Infrastructure;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QSMpv : CSMpvPort
{
    private readonly CSMpv csMpv;
    private readonly RoutedEventHandler qsMpvInstaller;
    private readonly RoutedEventHandler qsMpvOpener;
    private bool qsMpvEnabled;
    private Button? psMpvInstaller;
    private Button? psMpvOpener;
    private QSMpvAnswer? qsMpvAnswer;
    private QSOptionsOperation? qsOptionsOperation;
    private (Func<bool, Task>, Func<bool, string, Task>) qsMpvOutcome;

    public QSMpv(bool enabled)
    {
        csMpv = new CSMpv(this);
        qsMpvInstaller = QSMpvStart;
        qsMpvOpener = QSMpvOpen;
        QSMpvUpdate(enabled);
    }

    public QSMpv(bool enabled, CSMpvPort port)
    {
        csMpv = new CSMpv(port);
        qsMpvInstaller = QSMpvStart;
        qsMpvOpener = QSMpvOpen;
        QSMpvUpdate(enabled);
    }

    public event Action<bool>? QSMpvChanged;

    public bool QSMpvEnabled => qsMpvEnabled;

    public FrameworkElement QSMpvAttach(FrameworkElement surface, QSMpvAnswer answer)
    {
        QSMpvDetach();
        psMpvInstaller = QContract.QContractFind<Button>(surface, "PSMpvInstaller");
        psMpvOpener = QContract.QContractFind<Button>(surface, "PSMpvOpener");
        ProgressBar psMpvProgress = QContract.QContractFind<ProgressBar>(surface, "PSMpvProgress");
        qsMpvAnswer = answer;
        QSOptionsOperation operation = new(psMpvInstaller, psMpvProgress);
        qsOptionsOperation = operation;
        qsMpvOutcome = (
            installed => QSMpvUpdate(operation, installed, () => answer.QSMpvShow(
                true,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv"),
                QLocalizationCatalog.QLocalizationTextRead("Mpv.Local.Install.Completed"))),
            (installed, failure) => QSMpvUpdate(operation, installed, () => answer.QSMpvShow(
                false,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv"),
                QLocalizationCatalog.QLocalizationFormat("Mpv.Local.Install.Failed", failure))));

        QButton.QButtonAttach(psMpvInstaller, QButtonLook.QButtonLookWhite);
        QButton.QButtonAttach(psMpvOpener, QButtonLook.QButtonLookWhite);
        Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
        icon.Freeze();
        psMpvOpener.Content = new Image
        {
            Source = QIcon.QIconRead("/PAsset/PPanel/POpen.svg", icon),
            Width = 22,
            Height = 22,
            Stretch = Stretch.Uniform
        };
        psMpvOpener.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.System.Open");
        QSMpvUpdate(psMpvInstaller, csMpv.CSMpvInstalledCheck());
        psMpvInstaller.Click += qsMpvInstaller;
        psMpvOpener.Click += qsMpvOpener;
        return surface;
    }

    public void QSMpvDetach()
    {
        if (psMpvInstaller is not null)
        {
            psMpvInstaller.Click -= qsMpvInstaller;
            psMpvOpener!.Click -= qsMpvOpener;
        }

        qsOptionsOperation = null;
        qsMpvOutcome = default;
        psMpvInstaller = null;
        psMpvOpener = null;
        qsMpvAnswer = null;
    }

    public void QSMpvUpdate(bool enabled)
    {
        qsMpvEnabled = enabled;
        QSMpvChanged?.Invoke(enabled);
    }

    public async Task QSMpvUpdate()
    {
        bool available;
        try
        {
            available = await csMpv.CSMpvAvailabilityCheck();
        }
        catch (Exception exception)
        {
            LTraceLog.LTraceErrorRecord("Options mpv check failed", exception);
            available = false;
        }

        QSMpvUpdate(available);
    }

    public async Task QSMpvStart()
    {
        QSOptionsOperation operation = qsOptionsOperation!;
        if (operation is null)
        {
            throw new NullReferenceException();
        }
        IProgress<double> progress = new Progress<double>(value =>
            QSMpvOperationCheck(operation)?.QSOptionsOperationApply(value));
        try
        {
            await csMpv.CSMpvStart(
                progress,
                operation.QSOptionsOperationStart,
                qsMpvOutcome.Item1,
                qsMpvOutcome.Item2);
        }
        catch (Exception exception)
        {
            QSMpvOperationCheck(operation)?.QSOptionsOperationHide();
            if (QSMpvOperationCheck(operation) is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }
    }

    private async Task QSMpvUpdate(QSOptionsOperation operation, bool installed, Action show)
    {
        QSMpvOperationCheck(operation)?.QSOptionsOperationHide();
        QSMpvOperationCheck(operation)?.QSOptionsOperationApply(installer =>
        {
            QSMpvUpdate(installer, installed);
            installer.IsEnabled = true;
        });
        await QSMpvUpdate(operation);
        QSMpvOperationCheck(operation)?.QSOptionsOperationApply(_ => show());
    }

    public void QSMpvOpen() => qsMpvAnswer!.QSMpvOpen();

    private async void QSMpvStart(object sender, RoutedEventArgs _) => await QSMpvStart();

    private void QSMpvOpen(object sender, RoutedEventArgs _) => QSMpvOpen();

    private QSOptionsOperation? QSMpvOperationCheck(QSOptionsOperation operation) =>
        ReferenceEquals(qsOptionsOperation, operation) ? operation : null;

    private void QSMpvUpdate(Button installer, bool installed)
    {
        installer.Content = QLocalizationCatalog.QLocalizationTextRead(
            installed
                ? "Options.System.ReinstallMpv"
                : "Options.System.DownloadMpv");
    }

    private async Task QSMpvUpdate(QSOptionsOperation operation)
    {
        bool available;
        try
        {
            available = await csMpv.CSMpvAvailabilityCheck();
        }
        catch (Exception exception)
        {
            LTraceLog.LTraceErrorRecord("Options mpv check failed", exception);
            available = false;
        }

        if (QSMpvOperationCheck(operation) is not null)
        {
            QSMpvUpdate(available);
        }
    }

    bool CSMpvPort.CSMpvInstalledCheck() => LMpvInstall.LMpvInstalledCheck();

    Task<bool> CSMpvPort.CSMpvAvailabilityCheck() => LRenderer.LRendererMpvCheck();

    async Task<(bool, string, bool)> CSMpvPort.CSMpvStart(IProgress<double> progress)
    {
        LMpvInstallResult result = await LMpvInstall.LMpvInstallStart(progress);
        return (
            result.LMpvInstallSuccess,
            result.LMpvInstallMessage,
            LMpvInstall.LMpvInstalledCheck());
    }
}
