using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video plans built from the tools' plain values, as plan reads, so the gate hands them on without naming the
// plan record. Only the tools the gate put in become steps: the persistent plan runs in the tools' order, brightness,
// contrast, saturation, gamma, white balance, exposure, curve; a plan in the caller's order runs its processing step
// tokens instead.
public sealed class LVideoFacade : LVideoPort
{
    internal LVideoFacade()
    {
    }

    // One step per token that names a tool the gate put in, in the tokens' order; any other token is passed over.
    public LInspectorPort LVideoPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve)
    {
        ArgumentNullException.ThrowIfNull(lProcessing);
        List<LWorkVideoStep> lTools = LVideoStepsCreate(
            lBrightness, lContrast, lSaturation, lGamma, lWhitebalance, lExposure, lCurve);
        var lSteps = new List<LWorkVideoStep>();
        foreach (string lToken in lProcessing)
        {
            if (LColorToken.LColorTokenParse(lToken) is { } lKind
                && lTools.FirstOrDefault(lTool => lTool.LWorkStepKind == lKind) is { } lStep)
            {
                lSteps.Add(lStep);
            }
        }

        return new LInspectorOutlet(new LWorkVideo(lSteps));
    }

    public LInspectorPort LVideoPersistentCreate(
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve) =>
        new LInspectorOutlet(new LWorkVideo(LVideoStepsCreate(
            lBrightness, lContrast, lSaturation, lGamma, lWhitebalance, lExposure, lCurve)));

    // A step for each tool the gate put in, in the tools' order.
    private static List<LWorkVideoStep> LVideoStepsCreate(
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve)
    {
        var lSteps = new List<LWorkVideoStep>();
        if (lBrightness is { } lLight)
        {
            lSteps.Add(LWorkVideoStep.LWorkBrightnessCreate(lLight.LVideoActive, lLight.LVideoValue));
        }

        if (lContrast is { } lSpread)
        {
            lSteps.Add(LWorkVideoStep.LWorkContrastCreate(lSpread.LVideoActive, lSpread.LVideoValue));
        }

        if (lSaturation is { } lChroma)
        {
            lSteps.Add(LWorkVideoStep.LWorkSaturationCreate(lChroma.LVideoActive, lChroma.LVideoValue));
        }

        if (lGamma is { } lSlots)
        {
            lSteps.Add(LWorkVideoStep.LWorkGammaCreate(
                lSlots.LVideoActive,
                lSlots.LVideoValues[0],
                lSlots.LVideoValues[1],
                lSlots.LVideoValues[2],
                lSlots.LVideoValues[3],
                lSlots.LVideoValues[4]));
        }

        if (lWhitebalance is not null)
        {
            lSteps.Add(LWhitebalanceAnswer.LWhitebalanceStepCreate(lWhitebalance));
        }

        if (lExposure is { } lStops)
        {
            lSteps.Add(LWorkVideoStep.LWorkExposureCreate(lStops.LVideoActive, lStops.LVideoValue));
        }

        if (lCurve is { } lPoints)
        {
            lSteps.Add(LWorkVideoStep.LWorkCurveCreate(
                lPoints.LVideoActive,
                LVideoPointsCreate(lPoints.LVideoChannels[0]),
                LVideoPointsCreate(lPoints.LVideoChannels[1]),
                LVideoPointsCreate(lPoints.LVideoChannels[2]),
                LVideoPointsCreate(lPoints.LVideoChannels[3])));
        }

        return lSteps;
    }

    private static LWorkCurvePoint[] LVideoPointsCreate(IReadOnlyList<(double, double)> lChannel) =>
        lChannel.Select(lPoint => new LWorkCurvePoint(lPoint.Item1, lPoint.Item2)).ToArray();
}
