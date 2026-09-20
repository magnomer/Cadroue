using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LWhitebalance
{
    private static readonly LWhitebalanceMethod[] lWhitebalanceMethods =
    {
        LWhitebalanceMethod.LWhitebalanceMethodAverage,
        LWhitebalanceMethod.LWhitebalanceMethodMinmax,
        LWhitebalanceMethod.LWhitebalanceMethodMedian,
        LWhitebalanceMethod.LWhitebalanceMethodManual
    };

    private LWorkVideoStep lWhitebalanceStep = LWorkVideoStep.LWorkWhitebalanceCreate(false);
    private bool lWhitebalancePersistent;
    private bool lWhitebalanceCapable;
    private bool lWhitebalancePreview;
    private LNeutralWheel lWhitebalanceEstimate = new(0, 0, false);
    private bool lWhitebalanceToolArmed;
    private bool lWhitebalanceToolPending;
    private LNeutralTarget lWhitebalanceTarget = LNeutralTarget.LNeutralTargetGrey;
    private string lWhitebalanceStatus = string.Empty;

    public event Action? LWhitebalanceChange;

    public event Action<LWhitebalanceMethod>? LWhitebalanceEstimateChange;

    public event Action<bool, LNeutralTarget>? LWhitebalanceToolChange;

    public LWorkVideoStep LWhitebalanceStep => lWhitebalanceStep;

    public LWorkWhitebalanceSettings LWhitebalanceValue => lWhitebalanceStep.LWorkWhitebalanceRead();

    public LWhitebalanceMethod LWhitebalanceMethod => LWhitebalanceValue.LWorkWhitebalanceMethod;

    public int LWhitebalanceMethodIndex => Array.IndexOf(lWhitebalanceMethods, LWhitebalanceMethod);

    public bool LWhitebalanceManual =>
        LWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;

    public bool LWhitebalancePersistent => lWhitebalancePersistent;

    public bool LWhitebalanceCapable => lWhitebalanceCapable;

    public bool LWhitebalancePreview => lWhitebalancePreview;

    public bool LWhitebalanceToolArmed => lWhitebalanceToolArmed;

    public bool LWhitebalanceGreyArmed =>
        lWhitebalanceToolArmed && lWhitebalanceTarget == LNeutralTarget.LNeutralTargetGrey;

    public bool LWhitebalanceWhiteArmed =>
        lWhitebalanceToolArmed && lWhitebalanceTarget == LNeutralTarget.LNeutralTargetWhite;

    public LNeutralTarget LWhitebalanceTarget => lWhitebalanceTarget;

    public string LWhitebalanceStatus => lWhitebalanceStatus;

    public bool LWhitebalanceGuideShown => LWhitebalanceGuideRead().Length > 0;

    public string LWhitebalanceGuideRead() => lWhitebalanceToolArmed
        ? LLocalization.LLocalizationTextRead(lWhitebalanceTarget == LNeutralTarget.LNeutralTargetWhite
            ? "Inspector.Video.WhitebalanceGuideWhite"
            : "Inspector.Video.WhitebalanceGuide")
        : lWhitebalanceStatus;

    public LNeutralWheel LWhitebalanceWheelRead()
    {
        LWorkWhitebalanceSettings lValue = LWhitebalanceValue;
        return LWhitebalanceManual
            ? LNeutral.LNeutralWheelResolve(lValue.LWorkSampleRed, lValue.LWorkSampleGreen, lValue.LWorkSampleBlue)
            : lWhitebalanceEstimate;
    }

    public LNeutralDot LWhitebalanceDotRead(double lSize, double lDot) =>
        LNeutral.LNeutralDotResolve(LWhitebalanceWheelRead(), lSize, lDot);

    public LWhitebalanceReadout LWhitebalanceReadoutRead()
    {
        LWorkWhitebalanceSettings lValue = LWhitebalanceValue;
        LNeutralDisplay lDisplay = LNeutral.LNeutralDisplayResolve(
            LWhitebalanceManual, lValue.LWorkSampleRed, lValue.LWorkSampleGreen, lValue.LWorkSampleBlue);
        return new LWhitebalanceReadout(
            lDisplay.LNeutralDisplaySampled,
            (byte)lDisplay.LNeutralDisplayRed,
            (byte)lDisplay.LNeutralDisplayGreen,
            (byte)lDisplay.LNeutralDisplayBlue,
            lDisplay.LNeutralDisplaySampled
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceSample"),
                    lDisplay.LNeutralDisplayRed,
                    lDisplay.LNeutralDisplayGreen,
                    lDisplay.LNeutralDisplayBlue)
                : string.Empty);
    }

    public void LWhitebalanceStepSet(LWorkVideoStep lStep)
    {
        LWorkWhitebalanceSettings lValue = lStep.LWorkWhitebalanceRead();
        LWhitebalanceRaise(LWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            lStep.LWorkStepActive,
            lValue.LWorkWhitebalanceMethod,
            lValue.LWorkWhitebalanceSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue)));
    }

    public void LWhitebalanceActiveSet(bool lActive)
    {
        bool lChanged = LWhitebalanceStepApply(lWhitebalanceStep with { LWorkStepActive = lActive });
        if (!lActive)
        {
            lChanged |= LWhitebalanceToolApply(false);
        }

        LWhitebalanceRaise(lChanged);
    }

    public void LWhitebalanceMethodSelect(int lIndex)
    {
        if (lIndex < 0 || lIndex >= lWhitebalanceMethods.Length)
        {
            return;
        }

        LWhitebalanceMethodSet(lWhitebalanceMethods[lIndex]);
    }

    public void LWhitebalanceMethodSet(LWhitebalanceMethod lMethod)
    {
        LWorkWhitebalanceSettings lValue = LWhitebalanceValue;
        LWhitebalanceRaise(LWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            lWhitebalanceStep.LWorkStepActive,
            lMethod,
            lValue.LWorkWhitebalanceSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue)));
    }

    public void LWhitebalanceSaturationSet(double lSaturation)
    {
        LWorkWhitebalanceSettings lValue = LWhitebalanceValue;
        LWhitebalanceRaise(LWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            lWhitebalanceStep.LWorkStepActive,
            lValue.LWorkWhitebalanceMethod,
            lSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue)));
    }

    public void LWhitebalanceSampleSet(LNeutralSample lSample)
    {
        bool lChanged = LWhitebalanceToolApply(false);
        lChanged |= LWhitebalanceStatusApply(string.Empty);
        lChanged |= LWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            true,
            LWhitebalanceMethod.LWhitebalanceMethodManual,
            LWhitebalanceValue.LWorkWhitebalanceSaturation,
            lSample.LNeutralRedGain,
            lSample.LNeutralGreenGain,
            lSample.LNeutralBlueGain,
            lSample.LNeutralRed,
            lSample.LNeutralGreen,
            lSample.LNeutralBlue));
        LWhitebalanceRaise(lChanged);
    }

    public void LWhitebalanceReset()
    {
        bool lChanged = LWhitebalanceToolApply(false);
        lChanged |= LWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(lWhitebalanceStep.LWorkStepActive));
        LWhitebalanceRaise(lChanged);
    }

    public void LWhitebalanceEstimateSet(LNeutralWheel lEstimate)
    {
        if (LWhitebalanceManual || lWhitebalanceEstimate == lEstimate)
        {
            return;
        }

        lWhitebalanceEstimate = lEstimate;
        LWhitebalanceChange?.Invoke();
    }

    public void LWhitebalanceToolSet(bool lArmed, LNeutralTarget lTarget)
    {
        bool lChanged = LWhitebalanceToolApply(lArmed && lWhitebalanceCapable, lTarget);
        LWhitebalanceRaise(lChanged);
    }

    public void LWhitebalanceToolToggle(LNeutralTarget lTarget, bool lChecked)
    {
        if (!lChecked && !(lWhitebalanceToolArmed && lWhitebalanceTarget == lTarget))
        {
            return;
        }

        LWhitebalanceToolSet(lChecked, lTarget);
    }

    public void LWhitebalanceWheelHandle(bool lPressed, double lX, double lY, double lSize)
    {
        if (!lPressed || !lWhitebalanceCapable)
        {
            return;
        }

        LNeutralWheel lWheel = LNeutral.LNeutralCanvasResolve(lX, lY, lSize);
        LWhitebalanceSampleSet(LNeutral.LNeutralColorResolve(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY));
    }

    public void LWhitebalanceStatusSet(string lStatus) => LWhitebalanceRaise(LWhitebalanceStatusApply(lStatus));

    public void LWhitebalancePersistentSet(bool lPersistent)
    {
        if (lWhitebalancePersistent == lPersistent)
        {
            return;
        }

        lWhitebalancePersistent = lPersistent;
        LWhitebalanceChange?.Invoke();
    }

    public void LWhitebalanceCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        bool lChanged = lWhitebalanceCapable != lCapable || lWhitebalancePreview != lPreviewShown;
        lWhitebalanceCapable = lCapable;
        lWhitebalancePreview = lPreviewShown;
        if (!lCapable)
        {
            lChanged |= LWhitebalanceToolApply(false);
        }

        LWhitebalanceRaise(lChanged);
    }

    private bool LWhitebalanceStepApply(LWorkVideoStep lNormal)
    {
        if (lWhitebalanceStep == lNormal)
        {
            return false;
        }

        LWhitebalanceMethod lPrevious = LWhitebalanceMethod;
        lWhitebalanceStep = lNormal;
        if (lPrevious != LWhitebalanceMethod && !LWhitebalanceManual)
        {
            lWhitebalanceEstimate = new LNeutralWheel(0, 0, false);
            LWhitebalanceEstimateChange?.Invoke(LWhitebalanceMethod);
        }

        return true;
    }

    private bool LWhitebalanceToolApply(bool lArmed, LNeutralTarget? lTarget = null)
    {
        LNeutralTarget lNext = lTarget ?? lWhitebalanceTarget;
        if (lWhitebalanceToolArmed == lArmed && (!lArmed || lWhitebalanceTarget == lNext))
        {
            return false;
        }

        lWhitebalanceToolArmed = lArmed;
        lWhitebalanceTarget = lNext;
        lWhitebalanceToolPending = true;
        if (!lArmed)
        {
            lWhitebalanceStatus = string.Empty;
        }

        return true;
    }

    private bool LWhitebalanceStatusApply(string lStatus)
    {
        if (lWhitebalanceStatus == lStatus)
        {
            return false;
        }

        lWhitebalanceStatus = lStatus;
        return true;
    }

    private void LWhitebalanceRaise(bool lChanged)
    {
        if (lChanged)
        {
            LWhitebalanceChange?.Invoke();
        }

        if (!lWhitebalanceToolPending)
        {
            return;
        }

        lWhitebalanceToolPending = false;
        LWhitebalanceToolChange?.Invoke(lWhitebalanceToolArmed, lWhitebalanceTarget);
    }
}

public sealed record LWhitebalanceReadout(
    bool LWhitebalanceReadoutSampled,
    byte LWhitebalanceReadoutRed,
    byte LWhitebalanceReadoutGreen,
    byte LWhitebalanceReadoutBlue,
    string LWhitebalanceReadoutText);
