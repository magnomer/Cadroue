using System.Globalization;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public readonly record struct QSEncoderTier(string QSEncoderTierLabel, int QSEncoderTierWidth, int QSEncoderTierHeight);

public sealed partial class QSEncoder
{
    public const int QSEncoderDimensionMost = 7680;

    private static readonly QSEncoderTier[] qsEncoderTiers =
    [
        new("Source", 0, 0),
        new("480p", 854, 480),
        new("720p", 1280, 720),
        new("1080p", 1920, 1080),
        new("1440p", 2560, 1440),
        new("4K", 3840, 2160),
        new("8K", 7680, 4320)
    ];

    private string qsEncoderVideoEncoder = string.Empty;
    private string qsEncoderVideoRate = string.Empty;
    private int qsEncoderSizeTier;
    private int qsEncoderWidth;
    private int qsEncoderHeight;
    private IReadOnlyList<QSVerdictRow> qsEncoderVideoResults = [];

    public event Action? QSEncoderVideoChanged;
    public event Action? QSEncoderSizeChanged;

    public static IReadOnlyList<QSEncoderTier> QSEncoderTiers => qsEncoderTiers;

    public string QSEncoderVideoEncoder => qsEncoderVideoEncoder;

    public string QSEncoderVideoRate => qsEncoderVideoRate;

    public LCapabilityCodec QSEncoderVideoCodec =>
        LCapability.LCapabilityRead(LRepertoireVideo.LRepertoireTokenResolve(qsEncoderVideoEncoder) ?? string.Empty);

    public int QSEncoderSizeTier => qsEncoderSizeTier;

    public int QSEncoderWidth => qsEncoderWidth;

    public int QSEncoderHeight => qsEncoderHeight;

    public IReadOnlyList<QSVerdictRow> QSEncoderVideoResults => qsEncoderVideoResults;

    private void QSEncoderVideoPrepare()
    {
        qsEncoderVideoEncoder = qsEncoderDraft.LPresetVideo.LPresetEncoder;
        string[] lModes = QSEncoderVideoCodec.LCapabilityModeLabels;
        string lStored = qsEncoderDraft.LPresetVideo.LPresetRateControl;
        qsEncoderVideoRate = lModes.Contains(lStored) ? lStored : lModes[0];
        string[] lParts = qsEncoderDraft.LPresetVideo.LPresetSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length == 2
            && int.TryParse(lParts[0], out int lWidth) && lWidth > 0
            && int.TryParse(lParts[1], out int lHeight) && lHeight > 0)
        {
            qsEncoderWidth = lWidth;
            qsEncoderHeight = lHeight;
            qsEncoderSizeTier = QSEncoderTierResolve(lWidth, lHeight);
        }
    }

    public bool QSEncoderVideoSet(string lEncoder, string lRate)
    {
        bool lEncoderChanged = !string.Equals(qsEncoderVideoEncoder, lEncoder, StringComparison.Ordinal);
        qsEncoderVideoEncoder = lEncoder;
        string[] lModes = QSEncoderVideoCodec.LCapabilityModeLabels;
        string lResolved = lModes.Contains(lRate) ? lRate : lModes[0];
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
        CancellationToken lToken = QSEncoderScanStart(LTrialKind.LTrialKindVideo);
        var lAvailable = new List<string>();
        var lNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lRows = new List<QSVerdictRow>();
        IReadOnlyList<LRepertoireEncoder> lCandidates = LRepertoireVideo.LRepertoireEncodersRead();
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
                lFeed.Report(lTotal == 0 ? 1 : (double)lDone / lTotal);
            }

            if (lCandidateAvailable)
            {
                lAvailable.Add(lCandidate.LRepertoireText);
            }
        }

        lToken.ThrowIfCancellationRequested();
        LTrialCatalog.LTrialSetApply(lNames);
        if (!lAvailable.Contains(qsEncoderVideoEncoder)
            && lCandidates.Any(lCandidate => string.Equals(
                lCandidate.LRepertoireText, qsEncoderVideoEncoder, StringComparison.Ordinal)))
        {
            lAvailable.Insert(0, qsEncoderVideoEncoder);
        }

        qsEncoderVideoResults = lRows;
        QSVerdict.QSVerdictRecord("video", lRows);
        return lAvailable;
    }

    public static int QSEncoderTierResolve(int lWidth, int lHeight)
    {
        for (int lAt = 1; lAt < qsEncoderTiers.Length; lAt++)
        {
            (_, int lTierWidth, int lTierHeight) = qsEncoderTiers[lAt];
            if ((lTierWidth == lWidth && lTierHeight == lHeight) || (lTierWidth == lHeight && lTierHeight == lWidth))
            {
                return lAt;
            }
        }

        return -1;
    }

    public bool QSEncoderTierSelect(int lTier)
    {
        lTier = Math.Clamp(lTier, 0, qsEncoderTiers.Length - 1);
        if (lTier == qsEncoderSizeTier)
        {
            return false;
        }

        qsEncoderSizeTier = lTier;
        qsEncoderWidth = qsEncoderTiers[lTier].QSEncoderTierWidth;
        qsEncoderHeight = qsEncoderTiers[lTier].QSEncoderTierHeight;
        QSEncoderSizeChanged?.Invoke();
        return true;
    }

    public bool QSEncoderSizeSet(int lWidth, int lHeight)
    {
        lWidth = Math.Max(0, lWidth);
        lHeight = Math.Max(0, lHeight);
        int lTier = lWidth == 0 && lHeight == 0
            ? 0
            : lWidth > 0 && lHeight > 0 ? QSEncoderTierResolve(lWidth, lHeight) : -1;
        if (lWidth == qsEncoderWidth && lHeight == qsEncoderHeight && lTier == qsEncoderSizeTier)
        {
            return false;
        }

        qsEncoderWidth = lWidth;
        qsEncoderHeight = lHeight;
        qsEncoderSizeTier = lTier;
        QSEncoderSizeChanged?.Invoke();
        return true;
    }

    public string QSEncoderSizeFormat() =>
        qsEncoderSizeTier != 0 && qsEncoderWidth > 0 && qsEncoderHeight > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{qsEncoderWidth} × {qsEncoderHeight}")
            : QSEncoderSourceMode;
}
