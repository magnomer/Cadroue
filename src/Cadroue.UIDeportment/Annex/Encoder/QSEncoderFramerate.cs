using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderFramerate
{
    private readonly UserControl qsEncoderFramerateSurface;

    public QSEncoderFramerate(UserControl lSurface, string lFps)
    {
        qsEncoderFramerateSurface = lSurface;
        TextBox lValue = QSEncoderFramerateValue;
        lValue.Text = CSEncoderFramerate.CSEncoderFramerateFormat(lFps, QSEncoderFramerateSource);
        QTextbox.QTextboxAttach(lValue);
    }

    private static string QSEncoderFramerateSource =>
        QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source");

    private TextBlock QSEncoderFramerateCaption =>
        QContract.QContractFind<TextBlock>(qsEncoderFramerateSurface, "psEncoderFramerateCaption");

    private Slider QSEncoderFramerateScale =>
        QContract.QContractFind<Slider>(qsEncoderFramerateSurface, "psEncoderFramerateScale");

    private TextBox QSEncoderFramerateValue =>
        QContract.QContractFind<TextBox>(qsEncoderFramerateSurface, "psEncoderFramerateValue");

    private TextBlock QSEncoderFramerateHint =>
        QContract.QContractFind<TextBlock>(qsEncoderFramerateSurface, "psEncoderFramerateHint");

    private Brush QSEncoderFramerateMuted =>
        QContract.QContractSheetFind<Brush>(qsEncoderFramerateSurface, "psEncoderFramerateMuted");

    private Brush QSEncoderFramerateInk =>
        QContract.QContractSheetFind<Brush>(qsEncoderFramerateSurface, "psEncoderFramerateInk");

    public UserControl QSEncoderFramerateBuild()
    {
        QSEncoderFramerateHint.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.FpsSource");

        if (CSEncoderFramerate.CSEncoderFramerateCheck(QSEncoderFramerateValue.Text, QSEncoderFramerateSource))
        {
            QSEncoderFramerateApply();
        }
        else
        {
            QSEncoderFramerateHint.Visibility = Visibility.Collapsed;
        }

        QSEncoderFramerateScale.Maximum = CSEncoderFramerate.CSEncoderFramerateMost;
        QSEncoderFramerateScale.Value = CSEncoderFramerate.CSEncoderFramerateResolve(
            QSEncoderFramerateValue.Text, QSEncoderFramerateSource);
        QSlider.QSliderAttach(QSEncoderFramerateScale);
        QSEncoderFramerateAttach();
        QSEncoderFramerateCaption.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.FPS");
        return qsEncoderFramerateSurface;
    }

    public string QSEncoderFramerateRead() =>
        CSEncoderFramerate.CSEncoderFramerateRead(QSEncoderFramerateValue.Text, QSEncoderFramerateSource);

    private void QSEncoderFramerateAttach()
    {
        var lSync = new StrongBox<bool>(false);
        QSEncoderFramerateScale.ValueChanged += (_, _) => QSEncoderFramerateSelect(lSync);
        QSEncoderFramerateValue.TextChanged += (_, _) => QSEncoderFramerateMatch(lSync);
    }

    private void QSEncoderFramerateSelect(StrongBox<bool> lSync)
    {
        if (lSync.Value)
        {
            return;
        }

        lSync.Value = true;
        QSEncoderFramerateValue.Text = CSEncoderFramerate.CSEncoderFramerateFormat(
            QSEncoderFramerateScale.Value, QSEncoderFramerateSource);
        QSEncoderFramerateUpdate(CSEncoderFramerate.CSEncoderFramerateCheck(QSEncoderFramerateScale.Value));
        TextBox lValue = QSEncoderFramerateValue;
        lValue.CaretIndex = lValue.Text.Length;
        lSync.Value = false;
    }

    private void QSEncoderFramerateMatch(StrongBox<bool> lSync)
    {
        if (lSync.Value)
        {
            return;
        }

        bool lSource = CSEncoderFramerate.CSEncoderFramerateCheck(
            QSEncoderFramerateValue.Text, QSEncoderFramerateSource);
        lSync.Value = true;
        QSEncoderFramerateScale.Value = CSEncoderFramerate.CSEncoderFramerateResolve(
            QSEncoderFramerateValue.Text, QSEncoderFramerateSource);
        QSEncoderFramerateUpdate(lSource);
        lSync.Value = false;
    }

    private void QSEncoderFramerateApply()
    {
        QSEncoderFramerateValue.Text = QSEncoderFramerateSource;
        QSEncoderFramerateUpdate(true);
    }

    private void QSEncoderFramerateUpdate(bool lSource)
    {
        QSEncoderFramerateValue.Foreground = lSource ? QSEncoderFramerateMuted : QSEncoderFramerateInk;
        QSEncoderFramerateHint.Visibility = lSource ? Visibility.Visible : Visibility.Collapsed;
    }
}
