using System.Globalization;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public readonly record struct LSEncoderTier(string LSEncoderTierLabel, int LSEncoderTierWidth, int LSEncoderTierHeight);

public sealed partial class LSEncoder
{
    public const int LSEncoderDimensionMost = 7680;

    private static readonly LSEncoderTier[] lsEncoderTiers =
    [
        new("Source", 0, 0),
        new("480p", 854, 480),
        new("720p", 1280, 720),
        new("1080p", 1920, 1080),
        new("1440p", 2560, 1440),
        new("4K", 3840, 2160),
        new("8K", 7680, 4320)
    ];

    private string lsEncoderVideoEncoder = string.Empty;
    private string lsEncoderVideoRate = string.Empty;
    private int lsEncoderSizeTier;
    private int lsEncoderWidth;
    private int lsEncoderHeight;
    private IReadOnlyList<LSVerdictRow> lsEncoderVideoResults = [];

    public event Action? LSEncoderVideoChange;
    public event Action? LSEncoderSizeChange;

    public static IReadOnlyList<LSEncoderTier> LSEncoderTiers => lsEncoderTiers;

    public string LSEncoderVideoEncoder => lsEncoderVideoEncoder;

    public string LSEncoderVideoRate => lsEncoderVideoRate;

    public LCapabilityCodec LSEncoderVideoCodec =>
        LCapability.LCapabilityRead(LRepertoireCatalog.LRepertoireTokenResolve(lsEncoderVideoEncoder) ?? string.Empty);

    public int LSEncoderSizeTier => lsEncoderSizeTier;

    public int LSEncoderWidth => lsEncoderWidth;

    public int LSEncoderHeight => lsEncoderHeight;

    public IReadOnlyList<LSVerdictRow> LSEncoderVideoResults => lsEncoderVideoResults;

    private void LSEncoderVideoPrepare()
    {
        lsEncoderVideoEncoder = lsEncoderDraft.LPresetVideo.LPresetEncoder;
        string[] lModes = LSEncoderVideoCodec.LCapabilityModeLabels;
        string lStored = lsEncoderDraft.LPresetVideo.LPresetRateControl;
        lsEncoderVideoRate = lModes.Contains(lStored) ? lStored : lModes[0];
        string[] lParts = lsEncoderDraft.LPresetVideo.LPresetSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length == 2
            && int.TryParse(lParts[0], out int lWidth) && lWidth > 0
            && int.TryParse(lParts[1], out int lHeight) && lHeight > 0)
        {
            lsEncoderWidth = lWidth;
            lsEncoderHeight = lHeight;
            lsEncoderSizeTier = LSEncoderTierResolve(lWidth, lHeight);
        }
    }

    public bool LSEncoderVideoSet(string lEncoder, string lRate)
    {
        bool lEncoderChanged = !string.Equals(lsEncoderVideoEncoder, lEncoder, StringComparison.Ordinal);
        lsEncoderVideoEncoder = lEncoder;
        string[] lModes = LSEncoderVideoCodec.LCapabilityModeLabels;
        string lResolved = lModes.Contains(lRate) ? lRate : lModes[0];
        if (!lEncoderChanged && string.Equals(lsEncoderVideoRate, lResolved, StringComparison.Ordinal))
        {
            return false;
        }

        lsEncoderVideoRate = lResolved;
        LSEncoderVideoChange?.Invoke();
        return true;
    }

    public static string[] LSEncoderVideoRead(string lContainer, string lKeep)
    {
        bool lKnown = LRepertoireCatalog.LRepertoireContainerNames.Contains(lContainer);
        string[] lItems = LRepertoireCatalog.LRepertoireEncodersRead()
            .Where(lCandidate =>
                (!lKnown || LRepertoireCatalog.LRepertoireContainerCheck(lCandidate.LRepertoireText, lContainer))
                && LTrialSet.LTrialSetCheck(lCandidate))
            .Select(lCandidate => lCandidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(lKeep) || lItems.Contains(lKeep))
        {
            return lItems;
        }

        bool lFits = LRepertoireCatalog.LRepertoireEncodersRead()
                .Any(lCandidate => string.Equals(lCandidate.LRepertoireText, lKeep, StringComparison.Ordinal))
            && (!lKnown || LRepertoireCatalog.LRepertoireContainerCheck(lKeep, lContainer));
        return lFits ? [lKeep, .. lItems] : lItems;
    }

    public static bool LSEncoderVideoCheck(string lText)
    {
        foreach (LRepertoireEncoder lCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            if (string.Equals(lCandidate.LRepertoireText, lText, StringComparison.Ordinal))
            {
                return LTrialSet.LTrialSetCheck(lCandidate);
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<string>> LSEncoderVideoScan(IProgress<double> lFeed)
    {
        CancellationToken lToken = LSEncoderScanStart(LTrialKind.LTrialKindVideo);
        var lAvailable = new List<string>();
        var lNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lRows = new List<LSVerdictRow>();
        IReadOnlyList<LRepertoireEncoder> lCandidates = LRepertoireCatalog.LRepertoireEncodersRead();
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

                lRows.Add(new LSVerdictRow(
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
        LTrialSet.LTrialSetApply(lNames);
        if (!lAvailable.Contains(lsEncoderVideoEncoder)
            && lCandidates.Any(lCandidate => string.Equals(
                lCandidate.LRepertoireText, lsEncoderVideoEncoder, StringComparison.Ordinal)))
        {
            lAvailable.Insert(0, lsEncoderVideoEncoder);
        }

        lsEncoderVideoResults = lRows;
        LSVerdict.LSVerdictRecord("video", lRows);
        return lAvailable;
    }

    public static int LSEncoderTierResolve(int lWidth, int lHeight)
    {
        for (int lAt = 1; lAt < lsEncoderTiers.Length; lAt++)
        {
            (_, int lTierWidth, int lTierHeight) = lsEncoderTiers[lAt];
            if ((lTierWidth == lWidth && lTierHeight == lHeight) || (lTierWidth == lHeight && lTierHeight == lWidth))
            {
                return lAt;
            }
        }

        return -1;
    }

    public bool LSEncoderTierSelect(int lTier)
    {
        lTier = Math.Clamp(lTier, 0, lsEncoderTiers.Length - 1);
        if (lTier == lsEncoderSizeTier)
        {
            return false;
        }

        lsEncoderSizeTier = lTier;
        lsEncoderWidth = lsEncoderTiers[lTier].LSEncoderTierWidth;
        lsEncoderHeight = lsEncoderTiers[lTier].LSEncoderTierHeight;
        LSEncoderSizeChange?.Invoke();
        return true;
    }

    public bool LSEncoderSizeSet(int lWidth, int lHeight)
    {
        lWidth = Math.Max(0, lWidth);
        lHeight = Math.Max(0, lHeight);
        int lTier = lWidth == 0 && lHeight == 0
            ? 0
            : lWidth > 0 && lHeight > 0 ? LSEncoderTierResolve(lWidth, lHeight) : -1;
        if (lWidth == lsEncoderWidth && lHeight == lsEncoderHeight && lTier == lsEncoderSizeTier)
        {
            return false;
        }

        lsEncoderWidth = lWidth;
        lsEncoderHeight = lHeight;
        lsEncoderSizeTier = lTier;
        LSEncoderSizeChange?.Invoke();
        return true;
    }

    public string LSEncoderSizeFormat() =>
        lsEncoderSizeTier != 0 && lsEncoderWidth > 0 && lsEncoderHeight > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{lsEncoderWidth} × {lsEncoderHeight}")
            : LSEncoderSourceMode;
}
