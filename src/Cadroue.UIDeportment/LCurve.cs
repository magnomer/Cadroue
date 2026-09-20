using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LCurve
{
    private readonly List<LWorkCurvePoint>[] lCurveChannels =
    {
        LCurveIdentityCreate(),
        LCurveIdentityCreate(),
        LCurveIdentityCreate(),
        LCurveIdentityCreate()
    };

    private bool lCurveActive;
    private int lCurveChannel;
    private int lCurveSelected = 1;
    private bool lCurvePersistent;
    private bool lCurveCapable;
    private bool lCurvePreview;
    private LHistogramCounts? lCurveHistogram;
    private bool lCurveDragActive;

    public LCurve()
    {
        LCurveCanvas = new LCurveCanvas(this);
    }

    public event Action? LCurveChange;

    public LCurveCanvas LCurveCanvas { get; }

    public bool LCurveActive => lCurveActive;

    public int LCurveChannel => lCurveChannel;

    public int LCurveSelected => lCurveSelected;

    public bool LCurvePersistent => lCurvePersistent;

    public bool LCurveCapable => lCurveCapable;

    public bool LCurvePreview => lCurvePreview;

    public LHistogramCounts? LCurveHistogram => lCurveHistogram;

    public bool LCurveDragActive => lCurveDragActive;

    public IReadOnlyList<LWorkCurvePoint> LCurvePoints => lCurveChannels[lCurveChannel];

    public LWorkCurvePoint LCurvePointRead() => lCurveChannels[lCurveChannel][lCurveSelected];

    public double LCurveInputPercent => LCurvePointRead().LWorkCurveInput * 100;

    public double LCurveOutputPercent => LCurvePointRead().LWorkCurveOutput * 100;

    public LWorkVideoStep LCurveStepRead() => LWorkVideoStep.LWorkCurveCreate(
        lCurveActive,
        lCurveChannels[0].ToArray(),
        lCurveChannels[1].ToArray(),
        lCurveChannels[2].ToArray(),
        lCurveChannels[3].ToArray());

    public void LCurveStepSet(LWorkVideoStep lStep)
    {
        LWorkCurveSettings lCurve = lStep.LWorkCurveRead();
        bool lPointsChanged = !lCurveChannels[0].SequenceEqual(lCurve.LWorkCurveMaster)
            || !lCurveChannels[1].SequenceEqual(lCurve.LWorkCurveRed)
            || !lCurveChannels[2].SequenceEqual(lCurve.LWorkCurveGreen)
            || !lCurveChannels[3].SequenceEqual(lCurve.LWorkCurveBlue);
        if (!lPointsChanged && lCurveActive == lStep.LWorkStepActive)
        {
            return;
        }

        lCurveActive = lStep.LWorkStepActive;
        if (lPointsChanged)
        {
            lCurveChannels[0] = lCurve.LWorkCurveMaster.ToList();
            lCurveChannels[1] = lCurve.LWorkCurveRed.ToList();
            lCurveChannels[2] = lCurve.LWorkCurveGreen.ToList();
            lCurveChannels[3] = lCurve.LWorkCurveBlue.ToList();
            LCurveSelectedClamp();
        }

        LCurveChange?.Invoke();
    }

    public void LCurveActiveSet(bool lActive)
    {
        if (lCurveActive == lActive)
        {
            return;
        }

        lCurveActive = lActive;
        LCurveChange?.Invoke();
    }

    public void LCurveChannelSelect(int lChannel)
    {
        if (lChannel < 0)
        {
            return;
        }

        int lClamped = Math.Clamp(lChannel, 0, lCurveChannels.Length - 1);
        if (lCurveChannel == lClamped)
        {
            return;
        }

        lCurveChannel = lClamped;
        LCurveSelectedClamp();
        LCurveChange?.Invoke();
    }

    public void LCurvePointSelect(int lIndex)
    {
        int lClamped = Math.Clamp(lIndex, 0, LCurvePoints.Count - 1);
        if (lCurveSelected == lClamped)
        {
            return;
        }

        lCurveSelected = lClamped;
        LCurveChange?.Invoke();
    }

    public void LCurvePointSet(double lInput, double lOutput)
    {
        List<LWorkCurvePoint> lPoints = lCurveChannels[lCurveChannel];
        double lClampedInput = LColorCurve.LColorCurveClamp(lPoints, lCurveSelected, Math.Clamp(lInput, 0, 1));
        var lPoint = new LWorkCurvePoint(lClampedInput, Math.Clamp(lOutput, 0, 1));
        if (lPoints[lCurveSelected] == lPoint)
        {
            return;
        }

        lPoints[lCurveSelected] = lPoint;
        LCurveChange?.Invoke();
    }

    public void LCurvePointCommit(string lInputText, string lOutputText)
    {
        LWorkCurvePoint lPoint = LCurvePointRead();
        double lInput = LInspector.LInspectorValueCommit(lInputText, lPoint.LWorkCurveInput * 100, null, null) / 100;
        double lOutput = LInspector.LInspectorValueCommit(lOutputText, lPoint.LWorkCurveOutput * 100, null, null) / 100;
        LCurvePointSet(lInput, lOutput);
    }

    public void LCurvePressHandle(double lX, double lY, double lSize)
    {
        int lHit = LCurveCanvas.LCurveHitFind(lX, lY, lSize);
        if (lHit < 0)
        {
            (double lInput, double lOutput) = LCurveCanvas.LCurveValueResolve(lX, lY, lSize);
            LCurvePointAdd(lInput, lOutput);
        }
        else
        {
            LCurvePointSelect(lHit);
        }

        lCurveDragActive = true;
    }

    public bool LCurveMoveHandle(double lX, double lY, double lSize)
    {
        if (!lCurveDragActive)
        {
            return false;
        }

        (double lInput, double lOutput) = LCurveCanvas.LCurveValueResolve(lX, lY, lSize);
        LCurvePointSet(lInput, lOutput);
        return true;
    }

    public bool LCurveReleaseHandle()
    {
        if (!lCurveDragActive)
        {
            return false;
        }

        lCurveDragActive = false;
        return true;
    }

    public void LCurvePointAdd(double lInput, double lOutput)
    {
        lCurveSelected = LColorCurve.LColorCurveInsert(
            lCurveChannels[lCurveChannel], Math.Clamp(lInput, 0, 1), Math.Clamp(lOutput, 0, 1));
        LCurveChange?.Invoke();
    }

    public void LCurvePointDelete()
    {
        List<LWorkCurvePoint> lPoints = lCurveChannels[lCurveChannel];
        if (lPoints.Count <= 2 || lCurveSelected <= 0 || lCurveSelected >= lPoints.Count - 1)
        {
            return;
        }

        lPoints.RemoveAt(lCurveSelected);
        LCurveSelectedClamp();
        LCurveChange?.Invoke();
    }

    public void LCurveChannelReset()
    {
        lCurveChannels[lCurveChannel] = LCurveIdentityCreate();
        LCurveSelectedClamp();
        LCurveChange?.Invoke();
    }

    public void LCurveReset()
    {
        for (int lIndex = 0; lIndex < lCurveChannels.Length; lIndex++)
        {
            lCurveChannels[lIndex] = LCurveIdentityCreate();
        }

        LCurveSelectedClamp();
        LCurveChange?.Invoke();
    }

    public void LCurveHistogramSet(LHistogramCounts? lHistogram)
    {
        if (ReferenceEquals(lCurveHistogram, lHistogram))
        {
            return;
        }

        lCurveHistogram = lHistogram;
        LCurveChange?.Invoke();
    }

    public void LCurvePersistentSet(bool lPersistent)
    {
        if (lCurvePersistent == lPersistent)
        {
            return;
        }

        lCurvePersistent = lPersistent;
        LCurveChange?.Invoke();
    }

    public void LCurveCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (lCurveCapable == lCapable && lCurvePreview == lPreviewShown)
        {
            return;
        }

        lCurveCapable = lCapable;
        lCurvePreview = lPreviewShown;
        LCurveChange?.Invoke();
    }

    private void LCurveSelectedClamp() =>
        lCurveSelected = Math.Clamp(lCurveSelected, 0, LCurvePoints.Count - 1);

    private static List<LWorkCurvePoint> LCurveIdentityCreate() =>
        new() { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };
}
