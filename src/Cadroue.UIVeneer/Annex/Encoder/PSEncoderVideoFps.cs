using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSEncoderVideoFps
{
    private readonly record struct PSVideoScale(double PSVideoScaleRate, string PSVideoScaleValue);

    private static readonly PSVideoScale[] psVideoFpsScale = PSVideoScaleCreate();

    private readonly TextBox psVideoFpsCustom;
    private UIElement? psVideoFpsNotice;

    internal PSEncoderVideoFps(string pFps)
    {
        psVideoFpsCustom = PSEntry.PSEntryBuild(PSVideoFpsFormat(pFps), 120);
    }

    private static string PSVideoFpsFormat(string pFps) => PSVideoSourceCheck(pFps) ? string.Empty : pFps.Trim();

    private static PSVideoScale[] PSVideoScaleCreate()
    {
        var pList = new List<PSVideoScale>();
        for (int pAt = 1; pAt <= 240; pAt++)
        {
            pList.Add(new PSVideoScale(pAt, pAt.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (double pRate in new[] { 23.976, 29.97, 59.94, 119.88 })
        {
            pList.Add(new PSVideoScale(pRate, pRate.ToString(CultureInfo.InvariantCulture)));
        }

        var pSorted = pList.OrderBy(pEntry => pEntry.PSVideoScaleRate).ToList();
        pSorted.Insert(0, new PSVideoScale(0, string.Empty));
        return pSorted.ToArray();
    }

    private static bool PSVideoSourceCheck(string pFps)
    {
        string pTrim = pFps.Trim();
        return pTrim.Length == 0
            || string.Equals(pTrim, "Same as source", StringComparison.Ordinal)
            || string.Equals(
                pTrim,
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source"),
                StringComparison.Ordinal);
    }

    private static int PSVideoFpsResolve(string pText)
    {
        if (PSVideoSourceCheck(pText)
            || !double.TryParse(pText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pRate)
            || !double.IsFinite(pRate) || pRate <= 0)
        {
            return 0;
        }

        int pBest = 1;
        double pBestDiff = double.MaxValue;
        for (int pAt = 1; pAt < psVideoFpsScale.Length; pAt++)
        {
            double pDiff = Math.Abs(psVideoFpsScale[pAt].PSVideoScaleRate - pRate);
            if (pDiff < pBestDiff)
            {
                pBestDiff = pDiff;
                pBest = pAt;
            }
        }

        return pBest;
    }

    internal void PSVideoFpsBuild(Panel pHost)
    {
        psVideoFpsNotice = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.FpsSource"));

        if (PSVideoSourceCheck(psVideoFpsCustom.Text))
        {
            PSVideoFpsApply();
        }
        else
        {
            PSEncoderSource.PSVideoHintSet(psVideoFpsNotice,false);
        }

        Slider pSlider = PSFader.PSFaderCreate(0, psVideoFpsScale.Length - 1, PSVideoFpsResolve(psVideoFpsCustom.Text));
        PSVideoFpsAttach(pSlider);
        pHost.Children.Add(PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.FPS"),
            PSFader.PSFaderRowBuild(pSlider, psVideoFpsCustom)));
        pHost.Children.Add(psVideoFpsNotice);
    }

    private void PSVideoFpsAttach(Slider pSlider)
    {
        var pSync = new StrongBox<bool>(false);
        pSlider.ValueChanged += (_, _) => PSVideoSlideHandle(pSlider, pSync);
        psVideoFpsCustom.TextChanged += (_, _) => PSVideoFpsHandle(pSlider, pSync);
    }

    private void PSVideoSlideHandle(Slider pSlider, StrongBox<bool> pSync)
    {
        if (pSync.Value)
        {
            return;
        }

        int pAt = PSVideoFpsClamp(pSlider.Value);
        pSync.Value = true;
        if (pAt == 0)
        {
            PSVideoFpsApply();
        }
        else
        {
            PSVideoScaleSet(pAt);
            PSEncoderSource.PSVideoInkApply(psVideoFpsCustom, false);
            PSEncoderSource.PSVideoHintSet(psVideoFpsNotice,false);
        }

        PSVideoCaretSet();
        pSync.Value = false;
    }

    private void PSVideoFpsHandle(Slider pSlider, StrongBox<bool> pSync)
    {
        if (pSync.Value)
        {
            return;
        }

        bool pSource = PSVideoSourceCheck(psVideoFpsCustom.Text);
        pSync.Value = true;
        PSVideoFpsSelect(pSlider);
        PSEncoderSource.PSVideoInkApply(psVideoFpsCustom, pSource);
        PSEncoderSource.PSVideoHintSet(psVideoFpsNotice,pSource);
        pSync.Value = false;
    }

    private static int PSVideoFpsClamp(double pValue) =>
        Math.Clamp((int)Math.Round(pValue), 0, psVideoFpsScale.Length - 1);

    private void PSVideoScaleSet(int pAt) => psVideoFpsCustom.Text = psVideoFpsScale[pAt].PSVideoScaleValue;

    private void PSVideoCaretSet() => psVideoFpsCustom.CaretIndex = psVideoFpsCustom.Text.Length;

    private void PSVideoFpsSelect(Slider pSlider) => pSlider.Value = PSVideoFpsResolve(psVideoFpsCustom.Text);

    private void PSVideoFpsApply()
    {
        PSVideoFpsSet();
        PSEncoderSource.PSVideoInkApply(psVideoFpsCustom, true);
        PSEncoderSource.PSVideoHintSet(psVideoFpsNotice,true);
    }

    private void PSVideoFpsSet() =>
        psVideoFpsCustom.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source");

    internal string PSVideoFpsRead()
    {
        string pValue = psVideoFpsCustom.Text.Trim();
        return PSVideoSourceCheck(pValue) ? "Same as source" : pValue;
    }
}
