using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public readonly record struct LClinicKind(LFlawKind LClinicKindValue, string LClinicKindName);

public sealed class LClinic
{
    public const string LClinicSalvageStep = "Salvage";

    public static readonly IReadOnlyList<LClinicKind> LClinicKinds = new LClinicKind[]
    {
        new(LFlawKind.LFlawKindContainer, "Container"),
        new(LFlawKind.LFlawKindTruncation, "Truncation"),
        new(LFlawKind.LFlawKindTransport, "Transport"),
        new(LFlawKind.LFlawKindMetadata, "Metadata"),
        new(LFlawKind.LFlawKindIndex, "Index"),
        new(LFlawKind.LFlawKindFraming, "Framing"),
        new(LFlawKind.LFlawKindConfig, "Config"),
        new(LFlawKind.LFlawKindTiming, "Timing"),
        new(LFlawKind.LFlawKindSecondary, "Secondary"),
        new(LFlawKind.LFlawKindCoded, "Coded"),
        new(LFlawKind.LFlawKindFfvone, "Ffvone")
    };

    private readonly Dictionary<LFlawKind, LWorkFixStep> lClinicSteps = new();
    private readonly Dictionary<string, Dictionary<LFlawKind, LCheckupResult>> lClinicResults =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> lClinicProgress = new(StringComparer.OrdinalIgnoreCase);
    private LWorkFixSalvage lClinicSalvage = LWorkFixSalvage.LWorkSalvageCreate();
    private string? lClinicSource;
    private string? lClinicStep;
    private LFlawKind? lClinicKind;
    private bool lClinicMinimized;
    private int lClinicSaveDepth;

    public event Action? LClinicChange;
    public event Action? LClinicPlanChange;
    public event Action<bool>? LClinicMinimizeChange;

    public LClinic()
    {
        foreach ((LFlawKind lKind, string _) in LClinicKinds)
        {
            lClinicSteps[lKind] = new LWorkFixStep(lKind, false, false);
        }
    }

    public string? LClinicSource => lClinicSource;

    public string? LClinicStep => lClinicStep;

    public LFlawKind? LClinicKind => lClinicKind;

    public bool LClinicSalvageShown => lClinicStep == LClinicSalvageStep;

    public bool LClinicMinimized => lClinicMinimized;

    public bool LClinicSaveSuspended => lClinicSaveDepth > 0;

    public void LClinicSaveSuspend() => lClinicSaveDepth++;

    public void LClinicSaveResume() => lClinicSaveDepth = Math.Max(0, lClinicSaveDepth - 1);

    public LWorkFixSalvage LClinicSalvage => lClinicSalvage;

    public LWorkFixStep LClinicStepRead() =>
        lClinicKind is { } lKind ? lClinicSteps[lKind] : new LWorkFixStep(LFlawKind.LFlawKindContainer, false, false);

    public bool LClinicRepairCheck() => lClinicSteps.Values.Any(lStep => lStep.LWorkFixRepair);

    public bool LClinicScanCheck() =>
        LClinicResultRead().LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning;

    public double LClinicProgressRead() =>
        lClinicSource is { } lSource && lClinicProgress.TryGetValue(lSource, out double lValue) ? lValue : 0;

    public LCheckupResult LClinicResultRead()
    {
        if (lClinicKind is not { } lKind)
        {
            return new LCheckupResult(lClinicSource ?? string.Empty, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeUntested);
        }

        return lClinicSource is { } lSource
            && lClinicResults.TryGetValue(lSource, out Dictionary<LFlawKind, LCheckupResult>? lKinds)
            && lKinds.TryGetValue(lKind, out LCheckupResult lStored)
            ? lStored
            : new LCheckupResult(lClinicSource ?? string.Empty, lKind, LCheckupOutcome.LCheckupOutcomeUntested);
    }

    public void LClinicMinimizedSet(bool lMinimized)
    {
        if (lClinicMinimized == lMinimized)
        {
            return;
        }

        lClinicMinimized = lMinimized;
        LClinicMinimizeChange?.Invoke(lMinimized);
    }

    public void LClinicSourceSet(string? lSourcePath)
    {
        lClinicSource = string.IsNullOrWhiteSpace(lSourcePath) ? null : lSourcePath;
        LClinicChange?.Invoke();
    }

    public void LClinicStepSet(string? lStepName)
    {
        lClinicStep = lStepName;
        lClinicKind = LClinicKinds
            .Where(lEntry => lEntry.LClinicKindName == lStepName)
            .Select(lEntry => (LFlawKind?)lEntry.LClinicKindValue)
            .FirstOrDefault();
        LClinicChange?.Invoke();
    }

    public void LClinicActiveSet(bool lActive)
    {
        if (lClinicKind is not { } lKind || lClinicSteps[lKind].LWorkFixRepair == lActive)
        {
            return;
        }

        lClinicSteps[lKind] = lClinicSteps[lKind] with { LWorkFixRepair = lActive };
        lClinicSalvage = LClinicSalvageNormalize(lClinicSalvage);
        LClinicChange?.Invoke();
        LClinicPlanChange?.Invoke();
    }

    public void LClinicPersistentSet(bool lPersistent)
    {
        if (lClinicKind is not { } lKind || lClinicSteps[lKind].LWorkFixPersistent == lPersistent)
        {
            return;
        }

        lClinicSteps[lKind] = lClinicSteps[lKind] with { LWorkFixPersistent = lPersistent };
        LClinicChange?.Invoke();
        LClinicPlanChange?.Invoke();
    }

    public void LClinicSalvageSet(LWorkFixSalvage lSalvage)
    {
        LWorkFixSalvage lNormal = LClinicSalvageNormalize(lSalvage);
        if (lClinicSalvage == lNormal)
        {
            return;
        }

        lClinicSalvage = lNormal;
        LClinicChange?.Invoke();
        LClinicPlanChange?.Invoke();
    }

    public LWorkFix LClinicPlanRead()
    {
        var lSteps = new List<LWorkFixStep>();
        foreach ((LFlawKind lKind, string _) in LClinicKinds)
        {
            lSteps.Add(lClinicSteps[lKind]);
        }

        return new LWorkFix(lSteps) { LWorkFixSalvage = lClinicSalvage };
    }

    public void LClinicPlanApply(LWorkFix lPlan)
    {
        foreach (LWorkFixStep lStep in lPlan.LWorkFixSteps)
        {
            lClinicSteps[lStep.LWorkFixKind] = lStep;
        }

        lClinicSalvage = LClinicSalvageNormalize(lPlan.LWorkFixSalvage);
        LClinicChange?.Invoke();
    }

    public void LClinicResultSet(string lPath, LFlawKind lKind, LCheckupResult lResult)
    {
        if (!lClinicResults.TryGetValue(lPath, out Dictionary<LFlawKind, LCheckupResult>? lKinds))
        {
            lKinds = new Dictionary<LFlawKind, LCheckupResult>();
            lClinicResults[lPath] = lKinds;
        }

        lKinds[lKind] = lResult;
        if (lResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning)
        {
            lClinicProgress[lPath] = 0;
        }
        else
        {
            lClinicProgress.Remove(lPath);
        }

        if (LClinicSourceMatch(lPath) && lClinicKind == lKind)
        {
            LClinicChange?.Invoke();
        }
    }

    public void LClinicResultsRemove(IReadOnlyList<string> lPaths)
    {
        bool lShown = false;
        foreach (string lPath in lPaths)
        {
            lShown |= lClinicResults.Remove(lPath) && LClinicSourceMatch(lPath);
            lClinicProgress.Remove(lPath);
        }

        if (lShown)
        {
            LClinicChange?.Invoke();
        }
    }

    public void LClinicProgressSet(string lPath, double lValue)
    {
        lClinicProgress[lPath] = Math.Clamp(lValue, 0, 1);
        if (LClinicSourceMatch(lPath))
        {
            LClinicChange?.Invoke();
        }
    }

    private bool LClinicSourceMatch(string lPath) =>
        string.Equals(lPath, lClinicSource, StringComparison.OrdinalIgnoreCase);

    private LWorkFixSalvage LClinicSalvageNormalize(LWorkFixSalvage lSalvage) =>
        LClinicRepairCheck() ? lSalvage : lSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisSource };
}
