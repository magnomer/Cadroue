using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The encoder window's audio codec: the chosen encoder and rate, the encoders a container offers, and their trial.
public sealed class QSEncoderAudio
{
    private string qsEncoderAudioEncoder;
    private string qsEncoderAudioRate;
    private IReadOnlyList<QSVerdictRow> qsEncoderAudioResults = [];
    private CancellationTokenSource? qsEncoderAudioTrial;

    internal QSEncoderAudio(LPreset lDraft)
    {
        qsEncoderAudioEncoder = lDraft.LPresetAudio.LPresetEncoder;
        qsEncoderAudioRate = QSEncoderRateResolve(
            QSEncoderAudioCodec.LCapabilityModeLabels, lDraft.LPresetAudio.LPresetRateControl);
    }

    // The rate a codec keeps: the asked rate when the codec offers it, else the codec's first mode.
    private static string QSEncoderRateResolve(string[] lCodec, string lMode) =>
        lCodec.Contains(lMode) ? lMode : lCodec[0];

    public event Action? QSEncoderAudioChanged;

    public string QSEncoderAudioEncoder => qsEncoderAudioEncoder;

    public string QSEncoderAudioRate => qsEncoderAudioRate;

    public LCapabilityCodec QSEncoderAudioCodec =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(qsEncoderAudioEncoder));

    public IReadOnlyList<QSVerdictRow> QSEncoderAudioResults => qsEncoderAudioResults;

    public QSVerdict QSEncoderVerdictCreate() =>
        new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.AudioTitle"), qsEncoderAudioResults);

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
        string[] lItems = LRepertoireAudioCatalog.LRepertoireAudioCandidates
            .Where(lCandidate => QSEncoderAudioCheck(lCandidate.LRepertoireText)
                && (!lKnown || LRepertoireCatalog.LRepertoireAudioCheck(lCandidate.LRepertoireName, lContainer)))
            .Select(lCandidate => lCandidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(lKeep) || lItems.Contains(lKeep))
        {
            return lItems;
        }

        string? lName = LRepertoireAudioCatalog.LRepertoireAudioResolve(lKeep);
        bool lFits = lName is not null
            && (!lKnown || LRepertoireCatalog.LRepertoireAudioCheck(lName, lContainer));
        return lFits ? [lKeep, .. lItems] : lItems;
    }

    public static bool QSEncoderAudioCheck(string lText)
    {
        if (LRepertoireAudioCatalog.LRepertoireAudioResolve(lText) is not { } lName)
        {
            return true;
        }

        return LTrialCatalog.LTrialSetRead(LTrialKind.LTrialKindAudio) is { } lSet
            ? lSet.Contains(lName)
            : LInventory.LInventoryInstalledCheck(lName);
    }

    public async Task<IReadOnlyList<string>> QSEncoderAudioScan(IProgress<double> lFeed)
    {
        CancellationToken lToken = QSEncoderScanStart();
        (List<string> lAvailable, List<string> lNames, List<QSVerdictRow> lRows) =
            await QSEncoderAudioRun(lFeed, lToken);
        lToken.ThrowIfCancellationRequested();
        LTrialCatalog.LTrialSetApply(lNames, LTrialKind.LTrialKindAudio);
        QSEncoderAudioInsert(lAvailable);
        qsEncoderAudioResults = lRows;
        QSVerdictTrace.QSVerdictRecord("audio", lRows);
        return lAvailable;
    }

    // A new trial cancels the one still running; closing the window cancels the last one.
    private CancellationToken QSEncoderScanStart()
    {
        qsEncoderAudioTrial?.Cancel();
        qsEncoderAudioTrial = new CancellationTokenSource();
        return qsEncoderAudioTrial.Token;
    }

    internal void QSEncoderAudioCancel() => qsEncoderAudioTrial?.Cancel();

    private static async Task<(List<string>, List<string>, List<QSVerdictRow>)> QSEncoderAudioRun(
        IProgress<double> lFeed, CancellationToken lToken)
    {
        var lAvailable = new List<string>();
        var lNames = new List<string>();
        var lRows = new List<QSVerdictRow>();
        IReadOnlyList<LRepertoireAudio> lCandidates = LRepertoireAudioCatalog.LRepertoireAudioCandidates;
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
            lFeed.Report(lCandidates.Count == 0 ? 1 : (double)lDone / lCandidates.Count);
        }

        return (lAvailable, lNames, lRows);
    }

    private void QSEncoderAudioInsert(List<string> lAvailable)
    {
        if (!lAvailable.Contains(qsEncoderAudioEncoder)
            && LRepertoireAudioCatalog.LRepertoireAudioResolve(qsEncoderAudioEncoder) is not null)
        {
            lAvailable.Insert(0, qsEncoderAudioEncoder);
        }
    }
}
