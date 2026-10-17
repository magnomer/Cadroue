using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed partial class QSEncoder
{
    private string qsEncoderAudioEncoder = string.Empty;
    private string qsEncoderAudioRate = string.Empty;
    private IReadOnlyList<QSVerdictRow> qsEncoderAudioResults = [];

    public event Action? QSEncoderAudioChanged;

    private Action? qsEncoderHelpHandler;

    public string QSEncoderAudioEncoder => qsEncoderAudioEncoder;

    public string QSEncoderAudioRate => qsEncoderAudioRate;

    public LCapabilityCodec QSEncoderAudioCodec =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(qsEncoderAudioEncoder));

    public IReadOnlyList<QSVerdictRow> QSEncoderAudioResults => qsEncoderAudioResults;

    public void QSEncoderHelpAttach(Action lHandler) => qsEncoderHelpHandler = lHandler;

    private void QSEncoderHelpHandle(string lEncoder) => qsEncoderHelpHandler?.Invoke();

    private void QSEncoderAudioPrepare()
    {
        qsEncoderAudioEncoder = qsEncoderDraft.LPresetAudio.LPresetEncoder;
        qsEncoderAudioRate = QSEncoderRateResolve(
            QSEncoderAudioCodec.LCapabilityModeLabels, qsEncoderDraft.LPresetAudio.LPresetRateControl);
    }

    public bool QSEncoderAudioSet(string lEncoder, string lRate)
    {
        bool lEncoderChanged = !string.Equals(qsEncoderAudioEncoder, lEncoder, StringComparison.Ordinal);
        qsEncoderAudioEncoder = lEncoder;
        string lResolved = QSEncoderRateResolve(QSEncoderAudioCodec.LCapabilityModeLabels, lRate);
        if (!lEncoderChanged && string.Equals(qsEncoderAudioRate, lResolved, StringComparison.Ordinal))
        {
            return false;
        }

        qsEncoderAudioRate = lResolved;
        QSEncoderAudioChanged?.Invoke();
        return true;
    }

    public static string[] QSEncoderAudioRead(string lContainer, string lKeep)
    {
        bool lKnown = LRepertoireCatalog.LRepertoireContainerNames.Contains(lContainer);
        string[] lItems = LRepertoireCatalog.LRepertoireAudioCandidates
            .Where(lCandidate => QSEncoderAudioCheck(lCandidate.LRepertoireText)
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

    public static bool QSEncoderAudioCheck(string lText)
    {
        if (LRepertoireCatalog.LRepertoireAudioResolve(lText) is not { } lName)
        {
            return true;
        }

        return LTrialCatalog.LTrialSetRead(LTrialKind.LTrialKindAudio) is { } lSet
            ? lSet.Contains(lName)
            : LInventory.LInventoryInstalledCheck(lName);
    }

    public async Task<IReadOnlyList<string>> QSEncoderAudioScan(IProgress<double> lFeed)
    {
        CancellationToken lToken = QSEncoderScanStart(LTrialKind.LTrialKindAudio);
        (List<string> lAvailable, List<string> lNames, List<QSVerdictRow> lRows) =
            await QSEncoderAudioRun(lFeed, lToken);
        lToken.ThrowIfCancellationRequested();
        LTrialCatalog.LTrialSetApply(lNames, LTrialKind.LTrialKindAudio);
        QSEncoderAudioInsert(lAvailable);
        QSEncoderVerdictSet(LTrialKind.LTrialKindAudio, lRows);
        QSVerdict.QSVerdictRecord("audio", lRows);
        return lAvailable;
    }

    private static async Task<(List<string>, List<string>, List<QSVerdictRow>)> QSEncoderAudioRun(
        IProgress<double> lFeed, CancellationToken lToken)
    {
        var lAvailable = new List<string>();
        var lNames = new List<string>();
        var lRows = new List<QSVerdictRow>();
        IReadOnlyList<LRepertoireAudio> lCandidates = LRepertoireCatalog.LRepertoireAudioCandidates;
        int lDone = 0;
        foreach (LRepertoireAudio lCandidate in lCandidates)
        {
            LTrialResult lResult = await LTrial.LTrialRun(
                lCandidate.LRepertoireName, LTrialKind.LTrialKindAudio, lToken);
            lRows.Add(new QSVerdictRow(
                lCandidate.LRepertoireText, lCandidate.LRepertoireName, lResult.LTrialSuccess, lResult.LTrialMessage));
            if (lResult.LTrialSuccess)
            {
                lAvailable.Add(lCandidate.LRepertoireText);
                lNames.Add(lCandidate.LRepertoireName);
            }

            lDone++;
            lFeed.Report(QSEncoderProgressResolve(lDone, lCandidates.Count));
        }

        return (lAvailable, lNames, lRows);
    }

    private void QSEncoderAudioInsert(List<string> lAvailable)
    {
        if (!lAvailable.Contains(qsEncoderAudioEncoder)
            && LRepertoireCatalog.LRepertoireAudioResolve(qsEncoderAudioEncoder) is not null)
        {
            lAvailable.Insert(0, qsEncoderAudioEncoder);
        }
    }
}
