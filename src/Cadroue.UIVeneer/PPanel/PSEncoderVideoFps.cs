using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSNotice;
using static Cadroue.UIVeneer.PSCasement.PSFader;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private readonly record struct PSVideoScale(double PSVideoScaleRate, string PSVideoScaleValue);

    private static readonly PSVideoScale[] psVideoFpsScale = PSVideoScaleCreate();

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
                LLocalization.LLocalizationTextRead("Encoder.Sample.Source"),
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

    private void PSVideoFpsBuild(Panel pHost)
    {
        psVideoFpsNotice = PSNoticeBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Notice.FpsSource"));

        if (PSVideoSourceCheck(psVideoFpsCustom.Text))
        {
            PSVideoFpsApply();
        }
        else
        {
            psVideoFpsNotice.Visibility = Visibility.Collapsed;
        }

        Slider pSlider = PSFaderCreate(0, psVideoFpsScale.Length - 1, PSVideoFpsResolve(psVideoFpsCustom.Text));

        bool pSync = false;
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            int pAt = Math.Clamp((int)Math.Round(pSlider.Value), 0, psVideoFpsScale.Length - 1);
            pSync = true;
            if (pAt == 0)
            {
                PSVideoFpsApply();
            }
            else
            {
                psVideoFpsCustom.Text = psVideoFpsScale[pAt].PSVideoScaleValue;
                psVideoFpsCustom.Foreground = PSFieldText;
                psVideoFpsNotice.Visibility = Visibility.Collapsed;
            }
            psVideoFpsCustom.CaretIndex = psVideoFpsCustom.Text.Length;
            pSync = false;
        };
        psVideoFpsCustom.TextChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            bool pSource = PSVideoSourceCheck(psVideoFpsCustom.Text);
            pSync = true;
            pSlider.Value = PSVideoFpsResolve(psVideoFpsCustom.Text);
            psVideoFpsCustom.Foreground = pSource ? PSFieldMuted : PSFieldText;
            psVideoFpsNotice.Visibility = pSource ? Visibility.Visible : Visibility.Collapsed;
            pSync = false;
        };

        pHost.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.FPS"),
            PSFaderRowBuild(pSlider, psVideoFpsCustom)));
        pHost.Children.Add(psVideoFpsNotice);
    }

    private void PSVideoFpsApply()
    {
        psVideoFpsCustom.Text = LLocalization.LLocalizationTextRead("Encoder.Sample.Source");
        psVideoFpsCustom.Foreground = PSFieldMuted;
        if (psVideoFpsNotice is not null)
        {
            psVideoFpsNotice.Visibility = Visibility.Visible;
        }
    }

    private string PSVideoFpsRead()
    {
        string pValue = psVideoFpsCustom.Text.Trim();
        return PSVideoSourceCheck(pValue) ? "Same as source" : pValue;
    }
}
