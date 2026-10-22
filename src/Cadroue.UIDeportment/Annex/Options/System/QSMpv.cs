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
        qsOptionsOperation = new QSOptionsOperation(psMpvInstaller, psMpvProgress);

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
        QSMpvAnswer answer = qsMpvAnswer!;
        operation.QSOptionsOperationStart();
        IProgress<double> progress = new Progress<double>(value =>
            QSMpvOperationCheck(operation)?.QSOptionsOperationApply(value));
        bool success;
        string failure;
        bool installed;
        try
        {
            (success, failure, installed) = await csMpv.CSMpvStart(progress);
        }
        catch (Exception exception)
        {
            if (QSMpvOperationCheck(operation) is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            return;
        }
        finally
        {
            QSMpvOperationCheck(operation)?.QSOptionsOperationHide();
        }

        QSMpvOperationCheck(operation)?.QSOptionsOperationApply(installer =>
        {
            QSMpvUpdate(installer, installed);
            installer.IsEnabled = true;
        });
        await QSMpvUpdate(operation);
        QSMpvOperationCheck(operation)?.QSOptionsOperationApply(_ =>
        {
            string title = QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv");
            string message = success
                ? QLocalizationCatalog.QLocalizationTextRead("Mpv.Local.Install.Completed")
                : QLocalizationCatalog.QLocalizationFormat("Mpv.Local.Install.Failed", failure);
            answer.QSMpvShow(success, title, message);
        });
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
