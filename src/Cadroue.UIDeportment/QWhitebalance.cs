using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QWhitebalance
{
    private static readonly LWhitebalanceMethod[] qWhitebalanceMethods =
    {
        LWhitebalanceMethod.LWhitebalanceMethodAverage,
        LWhitebalanceMethod.LWhitebalanceMethodMinmax,
        LWhitebalanceMethod.LWhitebalanceMethodMedian,
        LWhitebalanceMethod.LWhitebalanceMethodManual
    };

    private LWorkVideoStep qWhitebalanceStep = LWorkVideoStep.LWorkWhitebalanceCreate(false);
    private bool qWhitebalancePersistent;
    private bool qWhitebalanceCapable;
    private bool qWhitebalancePreview;
    private LNeutralWheel qWhitebalanceEstimate = new(0, 0, false);
    private bool qWhitebalanceToolArmed;
    private bool qWhitebalanceToolPending;
    private LNeutralTarget qWhitebalanceTarget = LNeutralTarget.LNeutralTargetGrey;
    private string qWhitebalanceStatus = string.Empty;

    public event Action? QWhitebalanceChanged;

    public event Action<LWhitebalanceMethod>? QWhitebalanceEstimateChanged;

    public event Action<bool, LNeutralTarget>? QWhitebalanceToolChanged;

    public LWorkVideoStep QWhitebalanceStep => qWhitebalanceStep;

    public LWorkWhitebalanceSettings QWhitebalanceValue => qWhitebalanceStep.LWorkWhitebalanceRead();

    public LWhitebalanceMethod QWhitebalanceMethod => QWhitebalanceValue.LWorkWhitebalanceMethod;

    public int QWhitebalanceMethodIndex => Array.IndexOf(qWhitebalanceMethods, QWhitebalanceMethod);

    public bool QWhitebalanceManual =>
        QWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;

    public bool QWhitebalancePersistent => qWhitebalancePersistent;

    public bool QWhitebalanceCapable => qWhitebalanceCapable;

    public bool QWhitebalancePreview => qWhitebalancePreview;

    public bool QWhitebalanceToolArmed => qWhitebalanceToolArmed;

    public bool QWhitebalanceGreyArmed =>
        qWhitebalanceToolArmed && qWhitebalanceTarget == LNeutralTarget.LNeutralTargetGrey;

    public bool QWhitebalanceWhiteArmed =>
        qWhitebalanceToolArmed && qWhitebalanceTarget == LNeutralTarget.LNeutralTargetWhite;

    public LNeutralTarget QWhitebalanceTarget => qWhitebalanceTarget;

    public string QWhitebalanceStatus => qWhitebalanceStatus;

    public bool QWhitebalanceGuideShown => QWhitebalanceGuideRead().Length > 0;

    public string QWhitebalanceGuideRead() => qWhitebalanceToolArmed
        ? QLocalizationCatalog.QLocalizationTextRead(qWhitebalanceTarget == LNeutralTarget.LNeutralTargetWhite
            ? "Inspector.Video.WhitebalanceGuideWhite"
            : "Inspector.Video.WhitebalanceGuide")
        : qWhitebalanceStatus;

    public LNeutralWheel QWhitebalanceWheelRead()
    {
        LWorkWhitebalanceSettings lValue = QWhitebalanceValue;
        return QWhitebalanceManual
            ? LNeutral.LNeutralWheelResolve(lValue.LWorkSampleRed, lValue.LWorkSampleGreen, lValue.LWorkSampleBlue)
            : qWhitebalanceEstimate;
    }

    public LNeutralDot QWhitebalanceDotRead(double lSize, double lDot) =>
        LNeutral.LNeutralDotResolve(QWhitebalanceWheelRead(), lSize, lDot);

    public QWhitebalanceReadout QWhitebalanceReadoutRead()
    {
        LWorkWhitebalanceSettings lValue = QWhitebalanceValue;
        LNeutralDisplay lDisplay = LNeutral.LNeutralDisplayResolve(
            QWhitebalanceManual, lValue.LWorkSampleRed, lValue.LWorkSampleGreen, lValue.LWorkSampleBlue);
        return new QWhitebalanceReadout(
            lDisplay.LNeutralDisplaySampled,
            (byte)lDisplay.LNeutralDisplayRed,
            (byte)lDisplay.LNeutralDisplayGreen,
            (byte)lDisplay.LNeutralDisplayBlue,
            lDisplay.LNeutralDisplaySampled
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceSample"),
                    lDisplay.LNeutralDisplayRed,
                    lDisplay.LNeutralDisplayGreen,
                    lDisplay.LNeutralDisplayBlue)
                : string.Empty);
    }

    public void QWhitebalanceStepSet(LWorkVideoStep lStep)
    {
        LWorkWhitebalanceSettings lValue = lStep.LWorkWhitebalanceRead();
        QWhitebalanceRaise(QWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
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

    public void QWhitebalanceActiveSet(bool lActive)
    {
        bool lChanged = QWhitebalanceStepApply(qWhitebalanceStep with { LWorkStepActive = lActive });
        if (!lActive)
        {
            lChanged |= QWhitebalanceToolApply(false);
        }

        QWhitebalanceRaise(lChanged);
    }

    public void QWhitebalanceMethodSelect(int lIndex)
    {
        if (lIndex < 0 || lIndex >= qWhitebalanceMethods.Length)
        {
            return;
        }

        QWhitebalanceMethodSet(qWhitebalanceMethods[lIndex]);
    }

    public void QWhitebalanceMethodSet(LWhitebalanceMethod lMethod)
    {
        LWorkWhitebalanceSettings lValue = QWhitebalanceValue;
        QWhitebalanceRaise(QWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            qWhitebalanceStep.LWorkStepActive,
            lMethod,
            lValue.LWorkWhitebalanceSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue)));
    }

    public void QWhitebalanceSaturationSet(double lSaturation)
    {
        LWorkWhitebalanceSettings lValue = QWhitebalanceValue;
        QWhitebalanceRaise(QWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            qWhitebalanceStep.LWorkStepActive,
            lValue.LWorkWhitebalanceMethod,
            lSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue)));
    }

    public void QWhitebalanceSampleSet(LNeutralSample lSample)
    {
        bool lChanged = QWhitebalanceToolApply(false);
        lChanged |= QWhitebalanceStatusApply(string.Empty);
        lChanged |= QWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(
            true,
            LWhitebalanceMethod.LWhitebalanceMethodManual,
            QWhitebalanceValue.LWorkWhitebalanceSaturation,
            lSample.LNeutralRedGain,
            lSample.LNeutralGreenGain,
            lSample.LNeutralBlueGain,
            lSample.LNeutralRed,
            lSample.LNeutralGreen,
            lSample.LNeutralBlue));
        QWhitebalanceRaise(lChanged);
    }

    public void QWhitebalanceReset()
    {
        bool lChanged = QWhitebalanceToolApply(false);
        lChanged |= QWhitebalanceStepApply(LWorkVideoStep.LWorkWhitebalanceCreate(qWhitebalanceStep.LWorkStepActive));
        QWhitebalanceRaise(lChanged);
    }

    public void QWhitebalanceEstimateSet(LNeutralWheel lEstimate)
    {
        if (QWhitebalanceManual || qWhitebalanceEstimate == lEstimate)
        {
            return;
        }

        qWhitebalanceEstimate = lEstimate;
        QWhitebalanceChanged?.Invoke();
    }

    public void QWhitebalanceToolSet(bool lArmed, LNeutralTarget lTarget)
    {
        bool lChanged = QWhitebalanceToolApply(lArmed && qWhitebalanceCapable, lTarget);
        QWhitebalanceRaise(lChanged);
    }

    public void QWhitebalanceToolToggle(LNeutralTarget lTarget, bool lChecked)
    {
        if (!lChecked && !(qWhitebalanceToolArmed && qWhitebalanceTarget == lTarget))
        {
            return;
        }

        QWhitebalanceToolSet(lChecked, lTarget);
    }

    public void QWhitebalanceWheelHandle(bool lPressed, double lX, double lY, double lSize)
    {
        if (!lPressed || !qWhitebalanceCapable)
        {
            return;
        }

        LNeutralWheel lWheel = LNeutral.LNeutralCanvasResolve(lX, lY, lSize);
        QWhitebalanceSampleSet(LNeutral.LNeutralColorResolve(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY));
    }

    public void QWhitebalanceStatusSet(string lStatus) => QWhitebalanceRaise(QWhitebalanceStatusApply(lStatus));

    public void QWhitebalancePersistentSet(bool lPersistent)
    {
        if (qWhitebalancePersistent == lPersistent)
        {
            return;
        }

        qWhitebalancePersistent = lPersistent;
        QWhitebalanceChanged?.Invoke();
    }

    public void QWhitebalanceCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        bool lChanged = qWhitebalanceCapable != lCapable || qWhitebalancePreview != lPreviewShown;
        qWhitebalanceCapable = lCapable;
        qWhitebalancePreview = lPreviewShown;
        if (!lCapable)
        {
            lChanged |= QWhitebalanceToolApply(false);
        }

        QWhitebalanceRaise(lChanged);
    }

    private bool QWhitebalanceStepApply(LWorkVideoStep lNormal)
    {
        if (qWhitebalanceStep == lNormal)
        {
            return false;
        }

        LWhitebalanceMethod lPrevious = QWhitebalanceMethod;
        qWhitebalanceStep = lNormal;
        if (lPrevious != QWhitebalanceMethod && !QWhitebalanceManual)
        {
            qWhitebalanceEstimate = new LNeutralWheel(0, 0, false);
            QWhitebalanceEstimateChanged?.Invoke(QWhitebalanceMethod);
        }

        return true;
    }

    private bool QWhitebalanceToolApply(bool lArmed, LNeutralTarget? lTarget = null)
    {
        LNeutralTarget lNext = lTarget ?? qWhitebalanceTarget;
        if (qWhitebalanceToolArmed == lArmed && (!lArmed || qWhitebalanceTarget == lNext))
        {
            return false;
        }

        qWhitebalanceToolArmed = lArmed;
        qWhitebalanceTarget = lNext;
        qWhitebalanceToolPending = true;
        if (!lArmed)
        {
            qWhitebalanceStatus = string.Empty;
        }

        return true;
    }

    private bool QWhitebalanceStatusApply(string lStatus)
    {
        if (qWhitebalanceStatus == lStatus)
        {
            return false;
        }

        qWhitebalanceStatus = lStatus;
        return true;
    }

    private void QWhitebalanceRaise(bool lChanged)
    {
        if (lChanged)
        {
            QWhitebalanceChanged?.Invoke();
        }

        if (!qWhitebalanceToolPending)
        {
            return;
        }

        qWhitebalanceToolPending = false;
        QWhitebalanceToolChanged?.Invoke(qWhitebalanceToolArmed, qWhitebalanceTarget);
    }
}

public sealed record QWhitebalanceReadout(
    bool QWhitebalanceReadoutSampled,
    byte QWhitebalanceReadoutRed,
    byte QWhitebalanceReadoutGreen,
    byte QWhitebalanceReadoutBlue,
    string QWhitebalanceReadoutText);
