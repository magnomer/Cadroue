using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The encoder window's video codec: the chosen encoder and rate, the encoders a container offers, and their trial.
public sealed class QSEncoderVideo
{
    private readonly bool qsEncoderSmart;
    private string qsEncoderVideoEncoder;
    private string qsEncoderVideoRate;
    private IReadOnlyList<QSVerdictRow> qsEncoderVideoResults = [];
    private CancellationTokenSource? qsEncoderVideoTrial;

    internal QSEncoderVideo(LPreset lDraft, bool lSmart)
    {
        qsEncoderSmart = lSmart;
        qsEncoderVideoEncoder = lDraft.LPresetVideo.LPresetEncoder;
        qsEncoderVideoRate = QSEncoder.QSEncoderRateResolve(
            QSEncoderVideoCodec.LCapabilityModeLabels, lDraft.LPresetVideo.LPresetRateControl);
    }

    public event Action? QSEncoderVideoChanged;

    public string QSEncoderVideoEncoder => qsEncoderVideoEncoder;

    public string QSEncoderVideoRate => qsEncoderVideoRate;

    public LCapabilityCodec QSEncoderVideoCodec =>
        LCapability.LCapabilityRead(LRepertoireVideo.LRepertoireTokenResolve(qsEncoderVideoEncoder) ?? string.Empty);

    public IReadOnlyList<QSVerdictRow> QSEncoderVideoResults => qsEncoderVideoResults;

    public QSVerdict QSEncoderVerdictCreate() =>
        new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.VideoTitle"), qsEncoderVideoResults);

    public string QSEncoderNoticeResolve(string lMode)
    {
        if (lMode != "Smart")
        {
            return "Encoder.Video.Notice.Copied";
        }

        return qsEncoderSmart ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
    }

    public bool QSEncoderVideoSet(string lEncoder, string lRate)
    {
        bool lEncoderChanged = !string.Equals(qsEncoderVideoEncoder, lEncoder, StringComparison.Ordinal);
        qsEncoderVideoEncoder = lEncoder;
        string lResolved = QSEncoder.QSEncoderRateResolve(QSEncoderVideoCodec.LCapabilityModeLabels, lRate);
        if (!lEncoderChanged && string.Equals(qsEncoderVideoRate, lResolved, StringComparison.Ordinal))
        {
            return false;
        }

        qsEncoderVideoRate = lResolved;
        QSEncoderVideoChanged?.Invoke();
        return true;
    }

    public static string[] QSEncoderVideoRead(string lContainer, string lKeep)
    {
        bool lKnown = LRepertoireCatalog.LRepertoireContainerNames.Contains(lContainer);
        string[] lItems = LRepertoireVideo.LRepertoireEncodersRead()
            .Where(lCandidate =>
                (!lKnown || LRepertoireVideo.LRepertoireContainerCheck(lCandidate.LRepertoireText, lContainer))
                && LTrialCatalog.LTrialSetCheck(lCandidate))
            .Select(lCandidate => lCandidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(lKeep) || lItems.Contains(lKeep))
        {
            return lItems;
        }

        bool lFits = LRepertoireVideo.LRepertoireEncodersRead()
                .Any(lCandidate => string.Equals(lCandidate.LRepertoireText, lKeep, StringComparison.Ordinal))
            && (!lKnown || LRepertoireVideo.LRepertoireContainerCheck(lKeep, lContainer));
        return lFits ? [lKeep, .. lItems] : lItems;
    }

    public static bool QSEncoderVideoCheck(string lText)
    {
        foreach (LRepertoireEncoder lCandidate in LRepertoireVideo.LRepertoireEncodersRead())
        {
            if (string.Equals(lCandidate.LRepertoireText, lText, StringComparison.Ordinal))
            {
                return LTrialCatalog.LTrialSetCheck(lCandidate);
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<string>> QSEncoderVideoScan(IProgress<double> lFeed)
    {
        CancellationToken lToken = QSEncoderScanStart();
        IReadOnlyList<LRepertoireEncoder> lCandidates = LRepertoireVideo.LRepertoireEncodersRead();
        (List<string> lAvailable, HashSet<string> lNames, List<QSVerdictRow> lRows) =
            await QSEncoderVideoRun(lCandidates, lFeed, lToken);
        lToken.ThrowIfCancellationRequested();
        LTrialCatalog.LTrialSetApply(lNames);
        QSEncoderVideoInsert(lAvailable, lCandidates);
        qsEncoderVideoResults = lRows;
        QSVerdictTrace.QSVerdictRecord("video", lRows);
        return lAvailable;
    }

    // A new trial cancels the one still running; closing the window cancels the last one.
    private CancellationToken QSEncoderScanStart()
    {
        qsEncoderVideoTrial?.Cancel();
        qsEncoderVideoTrial = new CancellationTokenSource();
        return qsEncoderVideoTrial.Token;
    }

    internal void QSEncoderVideoCancel() => qsEncoderVideoTrial?.Cancel();

    private static async Task<(List<string>, HashSet<string>, List<QSVerdictRow>)> QSEncoderVideoRun(
        IReadOnlyList<LRepertoireEncoder> lCandidates, IProgress<double> lFeed, CancellationToken lToken)
    {
        var lAvailable = new List<string>();
        var lNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lRows = new List<QSVerdictRow>();
        int lTotal = lCandidates.Sum(lCandidate => lCandidate.LRepertoireTokens.Count);
        int lDone = 0;
        foreach (LRepertoireEncoder lCandidate in lCandidates)
        {
            bool lCandidateAvailable = false;
            foreach (string lEncoder in lCandidate.LRepertoireTokens)
            {
                LTrialResult lResult = await LTrial.LTrialRun(lEncoder, LTrialKind.LTrialKindVideo, lToken);
                lCandidateAvailable |= lResult.LTrialSuccess;
                if (lResult.LTrialSuccess)
                {
                    lNames.Add(lEncoder);
                }

                lRows.Add(new QSVerdictRow(
                    lCandidate.LRepertoireText, lEncoder, lResult.LTrialSuccess, lResult.LTrialMessage));
                lDone++;
                lFeed.Report(QSEncoder.QSEncoderProgressResolve(lDone, lTotal));
            }

            if (lCandidateAvailable)
            {
                lAvailable.Add(lCandidate.LRepertoireText);
            }
        }

        return (lAvailable, lNames, lRows);
    }

    private void QSEncoderVideoInsert(List<string> lAvailable, IReadOnlyList<LRepertoireEncoder> lCandidates)
    {
        if (!lAvailable.Contains(qsEncoderVideoEncoder)
            && lCandidates.Any(lCandidate => string.Equals(
                lCandidate.LRepertoireText, qsEncoderVideoEncoder, StringComparison.Ordinal)))
        {
            lAvailable.Insert(0, qsEncoderVideoEncoder);
        }
    }
}
