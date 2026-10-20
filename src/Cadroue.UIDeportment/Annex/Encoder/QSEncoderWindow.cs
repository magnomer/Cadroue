using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderWindow
{
    private const string QSEncoderWindowPlacement = "Encoder";
    private readonly Window qsEncoderWindowSurface;
    private readonly QSEncoder qsEncoderWindowOwner;
    private readonly Action qsEncoderWindowSummary;
    private readonly QSEncoderOutput qsEncoderWindowOutput;
    private readonly QSEncoderPicture qsEncoderWindowPicture;

    public QSEncoderWindow(
        Window lSurface, QSEncoder lOwner, Action lSummary, Action<QSVerdict> lVerdict)
    {
        qsEncoderWindowSurface = lSurface;
        qsEncoderWindowOwner = lOwner;
        qsEncoderWindowSummary = lSummary;
        qsEncoderWindowOutput = new QSEncoderOutput(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderOutput"), lOwner, QSEncoderWindowHandle);
        qsEncoderWindowPicture = new QSEncoderPicture(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderPicture"),
            lOwner.QSEncoderVideo,
            lOwner.QSEncoderSize,
            lVerdict);
        _ = qsEncoderWindowPicture.QSEncoderPictureBuild();
        _ = lOwner.QSEncoderAudio.QSEncoderAudioBuild(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderAudio"),
            lVerdict);
        QSEncoderWindowBuild();
        QSEncoderWindowHandle(qsEncoderWindowOutput.QSEncoderOutputRead());
        lOwner.QSEncoderHelpAttach(() => lSurface.Dispatcher.InvokeAsync(lOwner.QSEncoderAudio.QSEncoderAudioApply));
        lSurface.Closed += (_, _) => QSEncoderWindowClose();
        QSEncoderWindowRestore();
    }

    private void QSEncoderWindowBuild()
    {
        qsEncoderWindowSurface.Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Window.Title");
        var lTabs = QContract.QContractFind<TabControl>(qsEncoderWindowSurface, "psEncoderTabs");
        ((TabItem)lTabs.Items[0]).Header = QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Output");
        ((TabItem)lTabs.Items[1]).Header = QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Video");
        ((TabItem)lTabs.Items[2]).Header = QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Audio");
        Button lApply = QContract.QContractFind<Button>(qsEncoderWindowSurface, "psEncoderApplyButton");
        Button lOk = QContract.QContractFind<Button>(qsEncoderWindowSurface, "psEncoderOkButton");
        Button lCancel = QContract.QContractFind<Button>(qsEncoderWindowSurface, "psEncoderCancelButton");
        lApply.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Apply");
        lOk.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.OK");
        lCancel.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Cancel");
        QButton.QButtonBuild(lApply, QButtonLook.QButtonLookWhite);
        QButton.QButtonBuild(lOk, QButtonLook.QButtonLookWhite);
        QButton.QButtonBuild(lCancel, QButtonLook.QButtonLookWhite);
        lApply.Click += (_, _) => QSEncoderWindowApply();
        lOk.Click += (_, _) =>
        {
            QSEncoderWindowApply();
            qsEncoderWindowSurface.DialogResult = true;
        };
        lCancel.Click += (_, _) => qsEncoderWindowSurface.Close();
    }

    private void QSEncoderWindowHandle(string lContainer)
    {
        qsEncoderWindowPicture.QSEncoderPictureHandle(lContainer);
        qsEncoderWindowOwner.QSEncoderAudio.QSEncoderAudioHandle(lContainer);
    }

    private void QSEncoderWindowApply()
    {
        qsEncoderWindowOutput.QSEncoderOutputSave();
        qsEncoderWindowPicture.QSEncoderPictureCommit();
        qsEncoderWindowOwner.QSEncoderAudio.QSEncoderAudioCommit();
        qsEncoderWindowOwner.QSEncoderApply();
        qsEncoderWindowSummary();
    }

    private void QSEncoderWindowClose()
    {
        qsEncoderWindowOwner.QSEncoderClose();
        QSEncoderWindowSave();
    }

    private void QSEncoderWindowRestore()
    {
        if (QPlacement.QPlacementRead(
                QSEncoderWindowPlacement,
                out double lLeft,
                out double lTop,
                out double lWidth,
                out double lHeight))
        {
            QSashPlacement.QSashPlacementRestore(qsEncoderWindowSurface, lLeft, lTop, lWidth, lHeight);
        }
    }

    private void QSEncoderWindowSave() => QPlacement.QPlacementSave(
        QSEncoderWindowPlacement,
        qsEncoderWindowSurface.Left,
        qsEncoderWindowSurface.Top,
        qsEncoderWindowSurface.Width,
        qsEncoderWindowSurface.Height);
}
