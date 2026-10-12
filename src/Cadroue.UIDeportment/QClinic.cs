using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public readonly record struct QClinicKind(LFlawKind QClinicKindValue, string QClinicKindName);

public sealed class QClinic
{
    public const string QClinicSalvageStep = "Salvage";

    public static readonly IReadOnlyList<QClinicKind> QClinicKinds = new QClinicKind[]
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

    private readonly Dictionary<LFlawKind, LWorkFixStep> qClinicSteps = new();
    private readonly Dictionary<string, Dictionary<LFlawKind, LCheckupResult>> qClinicResults =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> qClinicProgress = new(StringComparer.OrdinalIgnoreCase);
    private LWorkFixSalvage qClinicSalvage = LWorkFixSalvage.LWorkSalvageCreate();
    private string? qClinicSource;
    private string? qClinicStep;
    private LFlawKind? qClinicKind;
    private bool qClinicMinimized;
    private int qClinicSaveDepth;

    public event Action? QClinicChanged;
    public event Action? QClinicPlanChanged;
    public event Action<bool>? QClinicMinimizeChanged;

    public QClinic()
    {
        foreach ((LFlawKind lKind, string _) in QClinicKinds)
        {
            qClinicSteps[lKind] = new LWorkFixStep(lKind, false, false);
        }
    }

    public string? QClinicSource => qClinicSource;

    public string? QClinicStep => qClinicStep;

    public LFlawKind? QClinicKind => qClinicKind;

    public bool QClinicSalvageShown => qClinicStep == QClinicSalvageStep;

    public bool QClinicStepShown => qClinicKind is not null;

    public bool QClinicKnown => QClinicSalvageShown || QClinicStepShown;

    public bool QClinicPersistentAllowed => QClinicKnown && !QClinicSalvageShown;

    public bool QClinicRepairChecked => QClinicStepShown && QClinicStepRead().LWorkFixRepair;

    public bool QClinicPersistentChecked => QClinicStepShown && QClinicStepRead().LWorkFixPersistent;

    public bool QClinicResultShown => !QClinicSalvageShown && QClinicStepShown;

    public bool QClinicScanShown => QClinicResultShown && QClinicScanCheck();

    public double QClinicProgressValue => QClinicScanShown ? QClinicProgressRead() : 0;

    public bool QClinicSalvageActive => qClinicSalvage.LWorkSalvageActive;

    public bool QClinicSalvagePersistent => qClinicSalvage.LWorkSalvagePersistent;

    public bool QClinicSalvageSeparate => qClinicSalvage.LWorkSalvageMode == LSalvageMode.LSalvageModeSeparate;

    public bool QClinicSalvageRejoin => !QClinicSalvageSeparate;

    public bool QClinicSalvageFixed => qClinicSalvage.LWorkSalvageBasis == LSalvageBasis.LSalvageBasisFixed;

    public bool QClinicSalvageSource => !QClinicSalvageFixed;

    public string QClinicSalvageText => LLocalization.LLocalizationTextRead(!QClinicRepairCheck()
        ? "Clinic.Salvage.Basis.None"
        : QClinicSalvageFixed
            ? "Clinic.Salvage.Basis.Fixed.Description"
            : "Clinic.Salvage.Basis.Source.Description");

    public string QClinicTitleRead() =>
        LLocalization.LLocalizationTextRead(QClinicKnown ? $"Processing.Step.{qClinicStep}" : "Clinic.Header.Title");

    public string QClinicSimpleRead() =>
        QClinicKnown ? LLocalization.LLocalizationTextRead($"Clinic.Step.{qClinicStep}.Simple") : string.Empty;

    public string QClinicTechnicalRead() =>
        QClinicKnown ? LLocalization.LLocalizationTextRead($"Clinic.Step.{qClinicStep}.Technical") : string.Empty;

    public string QClinicResultFormat() => LCheckupFormatter.LCheckupBodyFormat(
        QClinicResultRead(),
        new LCheckupStrings(
            LLocalization.LLocalizationTextRead("Clinic.Result.Empty"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Scanning"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Clean"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Failed"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Defect"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Evidence"),
            LLocalization.LLocalizationTextRead("Clinic.Result.Repair")));

    public bool QClinicMinimized => qClinicMinimized;

    public bool QClinicSaveSuspended => qClinicSaveDepth > 0;

    public void QClinicSaveSuspend() => qClinicSaveDepth++;

    public void QClinicSaveResume() => qClinicSaveDepth = Math.Max(0, qClinicSaveDepth - 1);

    public LWorkFixSalvage QClinicSalvage => qClinicSalvage;

    public LWorkFixStep QClinicStepRead() =>
        qClinicKind is { } lKind ? qClinicSteps[lKind] : new LWorkFixStep(LFlawKind.LFlawKindContainer, false, false);

    public bool QClinicRepairCheck() => qClinicSteps.Values.Any(lStep => lStep.LWorkFixRepair);

    public bool QClinicScanCheck() =>
        QClinicResultRead().LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning;

    public double QClinicProgressRead() =>
        qClinicSource is { } lSource && qClinicProgress.TryGetValue(lSource, out double lValue) ? lValue : 0;

    public LCheckupResult QClinicResultRead()
    {
        if (qClinicKind is not { } lKind)
        {
            return new LCheckupResult(
                qClinicSource ?? string.Empty, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeUntested);
        }

        return qClinicSource is { } lSource
            && qClinicResults.TryGetValue(lSource, out Dictionary<LFlawKind, LCheckupResult>? lKinds)
            && lKinds.TryGetValue(lKind, out LCheckupResult lStored)
            ? lStored
            : new LCheckupResult(qClinicSource ?? string.Empty, lKind, LCheckupOutcome.LCheckupOutcomeUntested);
    }

    public void QClinicMinimizedSet(bool lMinimized)
    {
        if (qClinicMinimized == lMinimized)
        {
            return;
        }

        qClinicMinimized = lMinimized;
        QClinicMinimizeChanged?.Invoke(lMinimized);
    }

    public void QClinicSourceSet(string? lSourcePath)
    {
        qClinicSource = string.IsNullOrWhiteSpace(lSourcePath) ? null : lSourcePath;
        QClinicChanged?.Invoke();
    }

    public void QClinicStepSet(string? lStepName)
    {
        qClinicStep = lStepName;
        qClinicKind = QClinicKinds
            .Where(lEntry => lEntry.QClinicKindName == lStepName)
            .Select(lEntry => (LFlawKind?)lEntry.QClinicKindValue)
            .FirstOrDefault();
        QClinicChanged?.Invoke();
    }

    public void QClinicActiveSet(bool lActive)
    {
        if (qClinicKind is not { } lKind || qClinicSteps[lKind].LWorkFixRepair == lActive)
        {
            return;
        }

        qClinicSteps[lKind] = qClinicSteps[lKind] with { LWorkFixRepair = lActive };
        qClinicSalvage = QClinicSalvageNormalize(qClinicSalvage);
        QClinicChanged?.Invoke();
        QClinicPlanChanged?.Invoke();
    }

    public void QClinicPersistentSet(bool lPersistent)
    {
        if (qClinicKind is not { } lKind || qClinicSteps[lKind].LWorkFixPersistent == lPersistent)
        {
            return;
        }

        qClinicSteps[lKind] = qClinicSteps[lKind] with { LWorkFixPersistent = lPersistent };
        QClinicChanged?.Invoke();
        QClinicPlanChanged?.Invoke();
    }

    public void QSalvageActiveSet(bool lActive) =>
        QClinicSalvageSet(qClinicSalvage with { LWorkSalvageActive = lActive });

    public void QSalvagePersistentSet(bool lPersistent) =>
        QClinicSalvageSet(qClinicSalvage with { LWorkSalvagePersistent = lPersistent });

    public void QSalvageModeSet(LSalvageMode lMode) =>
        QClinicSalvageSet(qClinicSalvage with { LWorkSalvageMode = lMode });

    public void QSalvageBasisSet(LSalvageBasis lBasis) =>
        QClinicSalvageSet(qClinicSalvage with { LWorkSalvageBasis = lBasis });

    public void QClinicSalvageSet(LWorkFixSalvage lSalvage)
    {
        LWorkFixSalvage lNormal = QClinicSalvageNormalize(lSalvage);
        if (qClinicSalvage == lNormal)
        {
            return;
        }

        qClinicSalvage = lNormal;
        QClinicChanged?.Invoke();
        QClinicPlanChanged?.Invoke();
    }

    public LWorkFix QClinicPlanRead()
    {
        var lSteps = new List<LWorkFixStep>();
        foreach ((LFlawKind lKind, string _) in QClinicKinds)
        {
            lSteps.Add(qClinicSteps[lKind]);
        }

        return new LWorkFix(lSteps) { LWorkFixSalvage = qClinicSalvage };
    }

    public void QClinicPlanApply(LWorkFix lPlan)
    {
        foreach (LWorkFixStep lStep in lPlan.LWorkFixSteps)
        {
            qClinicSteps[lStep.LWorkFixKind] = lStep;
        }

        qClinicSalvage = QClinicSalvageNormalize(lPlan.LWorkFixSalvage);
        QClinicChanged?.Invoke();
    }

    public void QClinicResultSet(string lPath, LFlawKind lKind, LCheckupResult lResult)
    {
        if (!qClinicResults.TryGetValue(lPath, out Dictionary<LFlawKind, LCheckupResult>? lKinds))
        {
            lKinds = new Dictionary<LFlawKind, LCheckupResult>();
            qClinicResults[lPath] = lKinds;
        }

        lKinds[lKind] = lResult;
        if (lResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning)
        {
            qClinicProgress[lPath] = 0;
        }
        else
        {
            qClinicProgress.Remove(lPath);
        }

        if (QClinicSourceMatch(lPath) && qClinicKind == lKind)
        {
            QClinicChanged?.Invoke();
        }
    }

    public void QClinicResultsRemove(IReadOnlyList<string> lPaths)
    {
        bool lShown = false;
        foreach (string lPath in lPaths)
        {
            lShown |= qClinicResults.Remove(lPath) && QClinicSourceMatch(lPath);
            qClinicProgress.Remove(lPath);
        }

        if (lShown)
        {
            QClinicChanged?.Invoke();
        }
    }

    public void QClinicProgressSet(string lPath, double lValue)
    {
        qClinicProgress[lPath] = Math.Clamp(lValue, 0, 1);
        if (QClinicSourceMatch(lPath))
        {
            QClinicChanged?.Invoke();
        }
    }

    private bool QClinicSourceMatch(string lPath) =>
        string.Equals(lPath, qClinicSource, StringComparison.OrdinalIgnoreCase);

    private LWorkFixSalvage QClinicSalvageNormalize(LWorkFixSalvage lSalvage) =>
        QClinicRepairCheck() ? lSalvage : lSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisSource };
}
