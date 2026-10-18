using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobVerdict
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;

    internal LJobVerdict(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
    }

    internal LWorkState? LJobValidateState { get; private set; }

    internal string LJobValidateMessage { get; private set; } = string.Empty;

    internal async Task LJobVerdictResolve(string pOutput)
    {
        LWorkMedia? pOutputMedia = await LScout.LScoutMediaRead(pOutput, lJobToken, lJobOwner.LRunnerLevel)
            .ConfigureAwait(false);
        if (pOutputMedia is null)
        {
            LJobValidateState = LWorkState.LWorkStateUnresolved;
            LJobValidateMessage =
                "Validation: the repaired output could not be re-probed; the defect is still present.";
        }
        else if (lJobItem.LWorkSourceMedia is { LWorkMediaVideo: true } && !pOutputMedia.LWorkMediaVideo)
        {
            LJobValidateState = LWorkState.LWorkStateBlocked;
            LJobValidateMessage =
                "Validation: the repaired output has no principal video content; recovery is blocked.";
        }
        else if (!await LScoutDecode.LScoutDecodeCheck(lJobOwner, pOutput, lJobToken).ConfigureAwait(false))
        {
            LJobValidateState = LWorkState.LWorkStatePartial;
            LJobValidateMessage =
                "Validation: the output re-probes but still reports decode errors; damage was reduced, not resolved.";
        }
        else
        {
            LJobValidateState = LWorkState.LWorkStateDone;
            LJobValidateMessage = string.Empty;
        }

        IReadOnlyList<LDossier> pRepairable =
            LFixPlan.LFixRepairResolve(lJobItem.LWorkDossiers, lJobItem.LWorkFixPlan);
        if (LJobValidateState == LWorkState.LWorkStateDone
            && pRepairable.Any(pDossier => pDossier.LDossierRepair == LFlawFfvone.LFlawReport))
        {
            LJobValidateState = LWorkState.LWorkStateUnresolved;
            LJobValidateMessage =
                "Validation: FFV1 slice-CRC mismatch confirmed; the defect is detected but cannot be corrected. " +
                "The file was copied unchanged.";
        }

        LRunnerTrace.LRunnerRecord(
            $"Validating '{lJobItem.LWorkOutputName}': re-probed '{Path.GetFileName(pOutput)}' → {LJobValidateState}");
    }
}
