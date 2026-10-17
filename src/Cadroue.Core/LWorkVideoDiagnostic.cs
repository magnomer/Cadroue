namespace Cadroue.Core;

public static class LWorkVideoDiagnostic
{
    public static string LWorkDiagnosticRead(LWorkVideoStep lStep)
    {
        string lSummary = $"{lStep.LWorkStepKind} " +
            $"{lStep.LWorkStepValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";
        if (lStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
        {
            LWorkWhitebalanceSettings lWhitebalance = lStep.LWorkWhitebalanceRead();
            string lMethod = lWhitebalance.LWorkWhitebalanceMethod switch
            {
                LWhitebalanceMethod.LWhitebalanceMethodAverage => "Average",
                LWhitebalanceMethod.LWhitebalanceMethodMinmax => "Minmax",
                LWhitebalanceMethod.LWhitebalanceMethodManual => "Manual",
                _ => "Median"
            };
            string lSaturation = lWhitebalance.LWorkWhitebalanceSaturation.ToString(
                "0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (lWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
            {
                string lGains = string.Join("/", new[]
                {
                    lWhitebalance.LWorkWhitebalanceRed,
                    lWhitebalance.LWorkWhitebalanceGreen,
                    lWhitebalance.LWorkWhitebalanceBlue
                }.Select(lGain => lGain.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
                string lSample = string.Join("/", new[]
                {
                    lWhitebalance.LWorkSampleRed,
                    lWhitebalance.LWorkSampleGreen,
                    lWhitebalance.LWorkSampleBlue
                });
                return $"{lSummary} (method {lMethod}, saturation {lSaturation}, gain {lGains}, sample {lSample})";
            }

            return $"{lSummary} (method {lMethod}, saturation {lSaturation})";
        }

        if (lStep.LWorkStepKind == LColorKind.LColorKindCurve)
        {
            LWorkCurveSettings lCurve = lStep.LWorkCurveRead();
            var lChannels = new List<string>();
            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveMaster))
            {
                lChannels.Add($"master {lCurve.LWorkCurveMaster.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveRed))
            {
                lChannels.Add($"red {lCurve.LWorkCurveRed.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveGreen))
            {
                lChannels.Add($"green {lCurve.LWorkCurveGreen.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveBlue))
            {
                lChannels.Add($"blue {lCurve.LWorkCurveBlue.Count}");
            }

            return lChannels.Count == 0
                ? "LColorKindCurve"
                : $"LColorKindCurve ({string.Join(", ", lChannels)})";
        }

        if (lStep.LWorkStepKind != LColorKind.LColorKindGamma)
        {
            return lSummary;
        }

        LWorkGammaSettings lGamma = lStep.LWorkGammaRead();
        var lDetails = new List<string>();
        if (lGamma.LWorkGammaRed != 0)
        {
            lDetails.Add(
                $"red {lGamma.LWorkGammaRed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaGreen != 0)
        {
            lDetails.Add(
                $"green {lGamma.LWorkGammaGreen.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaBlue != 0)
        {
            lDetails.Add(
                $"blue {lGamma.LWorkGammaBlue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaHighlight != 0)
        {
            string lHighlight = lGamma.LWorkGammaHighlight.ToString(
                "0.###", System.Globalization.CultureInfo.InvariantCulture);
            lDetails.Add($"highlight {lHighlight}");
        }

        return lDetails.Count == 0 ? lSummary : $"{lSummary} ({string.Join(", ", lDetails)})";
    }
}
