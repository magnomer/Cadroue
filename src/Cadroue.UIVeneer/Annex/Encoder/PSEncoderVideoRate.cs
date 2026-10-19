using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSEncoderVideoRate
{
    private readonly QSEncoderVideo lsEncoderVideo;
    private readonly Dictionary<string, ComboBox> psVideoExtraCombos = new(StringComparer.Ordinal);
    private TextBox? psVideoQualityBox;
    private Slider? psVideoSpeedSlider;
    private IReadOnlyList<string>? psVideoSpeedValues;

    internal PSEncoderVideoRate(QSEncoderVideo lsVideo) => lsEncoderVideo = lsVideo;

    internal StackPanel PSVideoRowsPanel { get; } = new();

    internal string PSVideoQualityRead() => PSEncoderValue.PSEncoderTrimRead(psVideoQualityBox);

    internal Dictionary<string, string> PSVideoExtrasRead() => PSEncoderValue.PSEncoderExtrasRead(psVideoExtraCombos);

    internal void PSVideoRowsRebuild()
    {
        PSVideoRowsClear();
        PSVideoRowsReset();
        PSVideoQualityBuild(lsEncoderVideo.QSEncoderVideoQuality);
        PSVideoCautionBuild(lsEncoderVideo.QSEncoderVideoCaution);
        PSVideoSpeedBuild(lsEncoderVideo.QSEncoderVideoSpeed);
        PSVideoExtraBuild(lsEncoderVideo.QSEncoderVideoExtras);
    }

    private void PSVideoRowsClear() => PSVideoRowsPanel.Children.Clear();

    private void PSVideoRowsReset()
    {
        psVideoQualityBox = null;
        psVideoSpeedSlider = null;
        psVideoSpeedValues = null;
        psVideoExtraCombos.Clear();
    }

    private void PSVideoCautionBuild(string pCaution)
    {
        if (!string.IsNullOrWhiteSpace(pCaution))
        {
            PSVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(pCaution));
        }
    }

    private void PSVideoQualityBuild(QSEncoderQuality? pQuality)
    {
        if (pQuality is null)
        {
            return;
        }

        psVideoQualityBox = PSEntry.PSEntryBuild(pQuality.QSEncoderQualityText, 120);
        if (pQuality.QSEncoderQualityMinimum is double pMinimum
            && pQuality.QSEncoderQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.QSEncoderQualityBitrate
                ? PSFaderBitrate.PSFaderBitrateBuild(
                    pMinimum, pMaximum, pQuality.QSEncoderQualityText, psVideoQualityBox)
                : PSFaderQuality.PSFaderQualityBuild(
                    pMinimum,
                    pMaximum,
                    pQuality.QSEncoderQualityStep,
                    pQuality.QSEncoderQualityText,
                    psVideoQualityBox,
                    pQuality.QSEncoderQualityAscending);
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.QSEncoderQualityLabel, pSliderRow));
        }
        else
        {
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.QSEncoderQualityLabel, psVideoQualityBox));
        }

        PSVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(pQuality.QSEncoderQualityRange));
    }

    private void PSVideoSpeedBuild(QSEncoderSpeed? pSpeed)
    {
        if (pSpeed is null)
        {
            return;
        }

        IReadOnlyList<string> pLabels = pSpeed.QSEncoderSpeedLabels;
        psVideoSpeedValues = pSpeed.QSEncoderSpeedValues;
        Slider pSlider = PSVideoSliderBuild(pLabels, pSpeed.QSEncoderSpeedIndex);
        psVideoSpeedSlider = pSlider;
        TextBlock pText = PSVideoLabelBuild(pLabels[pSpeed.QSEncoderSpeedIndex]);
        PSVideoSliderAttach(pSlider, pText, pLabels);
        PSVideoRowsPanel.Children.Add(
            PSField.PSFieldBuild(pSpeed.QSEncoderSpeedLabel, PSFader.PSFaderRowBuild(pSlider, pText)));
    }

    private static Slider PSVideoSliderBuild(IReadOnlyList<string> pLabels, int pIndex)
    {
        int pLast = pLabels.Count - 1;
        return PSFader.PSFaderCreate(0, pLast, pLast - pIndex);
    }

    private static TextBlock PSVideoLabelBuild(string pLabel) => new()
    {
        Text = pLabel,
        Foreground = PSField.PSFieldText,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void PSVideoSliderAttach(Slider pSlider, TextBlock pText, IReadOnlyList<string> pLabels)
    {
        int pLast = pLabels.Count - 1;
        pSlider.ValueChanged += (_, _) => pText.Text = pLabels[PSVideoIndexResolve(pSlider.Value, pLast)];
    }

    private static int PSVideoIndexResolve(double pValue, int pLast) =>
        pLast - Math.Clamp((int)Math.Round(pValue), 0, pLast);

    internal string PSVideoSpeedRead()
    {
        if (psVideoSpeedSlider is null || psVideoSpeedValues is null || psVideoSpeedValues.Count == 0)
        {
            return string.Empty;
        }

        int pLast = psVideoSpeedValues.Count - 1;
        return psVideoSpeedValues[PSVideoIndexResolve(psVideoSpeedSlider.Value, pLast)];
    }

    private void PSVideoExtraBuild(IReadOnlyList<QSEncoderExtra> pExtras)
    {
        foreach (QSEncoderExtra pExtra in pExtras)
        {
            ComboBox pCombo = PSCombo.PSComboBuild(pExtra.QSEncoderExtraSelected, pExtra.QSEncoderExtraChoices);
            psVideoExtraCombos[pExtra.QSEncoderExtraOption] = pCombo;
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pExtra.QSEncoderExtraLabel, pCombo));
        }
    }
}
