using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed partial class LSEncoder
{
    private string lsEncoderAudioEncoder = string.Empty;
    private string lsEncoderAudioRate = string.Empty;
    private IReadOnlyList<LSVerdictRow> lsEncoderAudioResults = [];

    public event Action? LSEncoderAudioChange;

    public string LSEncoderAudioEncoder => lsEncoderAudioEncoder;

    public string LSEncoderAudioRate => lsEncoderAudioRate;

    public LCapabilityCodec LSEncoderAudioCodec =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(lsEncoderAudioEncoder));

    public IReadOnlyList<LSVerdictRow> LSEncoderAudioResults => lsEncoderAudioResults;

    private void LSEncoderAudioPrepare()
    {
        lsEncoderAudioEncoder = lsEncoderDraft.LPresetAudio.LPresetEncoder;
        string[] lModes = LSEncoderAudioCodec.LCapabilityModeLabels;
        string lStored = lsEncoderDraft.LPresetAudio.LPresetRateControl;
        lsEncoderAudioRate = lModes.Contains(lStored) ? lStored : lModes[0];
    }

    public bool LSEncoderAudioSet(string lEncoder, string lRate)
    {
        bool lEncoderChanged = !string.Equals(lsEncoderAudioEncoder, lEncoder, StringComparison.Ordinal);
        lsEncoderAudioEncoder = lEncoder;
        string[] lModes = LSEncoderAudioCodec.LCapabilityModeLabels;
        string lResolved = lModes.Contains(lRate) ? lRate : lModes[0];
        if (!lEncoderChanged && string.Equals(lsEncoderAudioRate, lResolved, StringComparison.Ordinal))
        {
            return false;
        }

        lsEncoderAudioRate = lResolved;
        LSEncoderAudioChange?.Invoke();
        return true;
    }

    public static string[] LSEncoderAudioRead(string lContainer, string lKeep)
    {
        bool lKnown = LRepertoireCatalog.LRepertoireContainerNames.Contains(lContainer);
        string[] lItems = LRepertoireCatalog.LRepertoireAudioCandidates
            .Where(lCandidate => LSEncoderAudioCheck(lCandidate.LRepertoireText)
                && (!lKnown || LRepertoireCatalog.LRepertoireAudioCheck(lCandidate.LRepertoireName, lContainer)))
            .Select(lCandidate => lCandidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(lKeep) || lItems.Contains(lKeep))
        {
            return lItems;
        }

        string? lName = LRepertoireCatalog.LRepertoireAudioResolve(lKeep);
        bool lFits = lName is not null
            && (!lKnown || LRepertoireCatalog.LRepertoireAudioCheck(lName, lContainer));
        return lFits ? [lKeep, .. lItems] : lItems;
    }

    public static bool LSEncoderAudioCheck(string lText)
    {
        if (LRepertoireCatalog.LRepertoireAudioResolve(lText) is not { } lName)
        {
            return true;
        }

        return LTrialSet.LTrialSetRead(LTrialKind.LTrialKindAudio) is { } lSet
            ? lSet.Contains(lName)
            : LInventory.LInventoryInstalledCheck(lName);
    }

    public async Task<IReadOnlyList<string>> LSEncoderAudioScan(IProgress<double> lFeed)
    {
        CancellationToken lToken = LSEncoderScanStart(LTrialKind.LTrialKindAudio);
        var lAvailable = new List<string>();
        var lNames = new List<string>();
        var lRows = new List<LSVerdictRow>();
        IReadOnlyList<LRepertoireAudio> lCandidates = LRepertoireCatalog.LRepertoireAudioCandidates;
        int lDone = 0;
        foreach (LRepertoireAudio lCandidate in lCandidates)
        {
            LTrialResult lResult = await LTrial.LTrialRun(
                lCandidate.LRepertoireName, LTrialKind.LTrialKindAudio, lToken);
            lRows.Add(new LSVerdictRow(
                lCandidate.LRepertoireText, lCandidate.LRepertoireName, lResult.LTrialSuccess, lResult.LTrialMessage));
            if (lResult.LTrialSuccess)
            {
                lAvailable.Add(lCandidate.LRepertoireText);
                lNames.Add(lCandidate.LRepertoireName);
            }

            lDone++;
            lFeed.Report(lCandidates.Count == 0 ? 1 : (double)lDone / lCandidates.Count);
        }

        lToken.ThrowIfCancellationRequested();
        LTrialSet.LTrialSetApply(lNames, LTrialKind.LTrialKindAudio);
        if (!lAvailable.Contains(lsEncoderAudioEncoder)
            && LRepertoireCatalog.LRepertoireAudioResolve(lsEncoderAudioEncoder) is not null)
        {
            lAvailable.Insert(0, lsEncoderAudioEncoder);
        }

        lsEncoderAudioResults = lRows;
        LSVerdict.LSVerdictRecord("audio", lRows);
        return lAvailable;
    }
}
