using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LCompassButton(
    string LCompassButtonKey,
    string LCompassButtonIcon,
    string LCompassButtonLabel,
    string LCompassButtonTooltip,
    string LCompassButtonAccent);

public sealed record LCompassGroup(IReadOnlyList<LCompassButton> LCompassGroupButtons, bool LCompassGroupSection);

public sealed class LCompass
{
    private sealed record LCompassEntry(string LCompassEntryKey, string LCompassEntryIcon, string LCompassEntryAccent);

    private sealed record LCompassSet(IReadOnlyList<LCompassEntry> LCompassSetEntries, bool LCompassSetSection);

    public const string LCompassPlayKey = "Play";
    public const string LCompassAccentPositive = "Positive";
    public const string LCompassAccentNegative = "Negative";
    public const string LCompassAccentNone = "";
    public const double LCompassVolumeMinimum = 0;
    public const double LCompassVolumeMaximum = 100;

    private static readonly LCompassEntry LCompassPlayEntry =
        new(LCompassPlayKey, "PCompassPlay.svg", LCompassAccentPositive);

    private static readonly IReadOnlyList<LCompassSet> LCompassSets =
    [
        new(
            [
                new("ZoomIn", "PCompassZoomIncrease.svg", LCompassAccentNone),
                new("ZoomOut", "PCompassZoomDecrease.svg", LCompassAccentNone),
            ],
            false),
        new([LCompassPlayEntry], false),
        new(
            [
                new("SectionAdd", "PCompassSectionAdd.svg", LCompassAccentNone),
                new("SectionDelete", "PCompassRemove.svg", LCompassAccentNegative),
            ],
            true),
        new(
            [
                new("SectionStart", "PCompassStart.svg", LCompassAccentNone),
                new("SectionSplit", "PCompassSplit.svg", LCompassAccentNone),
                new("SectionEnd", "PCompassEnd.svg", LCompassAccentNone),
            ],
            true),
        new(
            [
                new("KeyframePrevious", "PCompassKeyframePrevious.svg", LCompassAccentNone),
                new("KeyframeNearest", "PCompassKeyframeNear.svg", LCompassAccentNone),
                new("KeyframeNext", "PCompassKeyframeNext.svg", LCompassAccentNone),
            ],
            false),
    ];

    private readonly LFlow lCompassFlow;
    private readonly LViewer lCompassViewer;
    private readonly bool lCompassSectionShown;

    public LCompass(LFlow lFlow, LViewer lViewer, bool lSectionShown)
    {
        lCompassFlow = lFlow;
        lCompassViewer = lViewer;
        lCompassSectionShown = lSectionShown;
    }

    public bool LCompassPlaying => lCompassViewer.LViewerPlaying;

    public double LCompassVolume => lCompassViewer.LViewerVolume;

    public bool LCompassWaveformActive => lCompassFlow.LFlowWaveformActive;

    public bool LCompassEditActive => lCompassFlow.LFlowSectionEditable;

    public IReadOnlyList<LCompassGroup> LCompassGroupsRead() =>
        LCompassSets
            .Where(lSet => lCompassSectionShown || !lSet.LCompassSetSection)
            .Select(lSet => new LCompassGroup(
                lSet.LCompassSetEntries.Select(LCompassButtonCreate).ToArray(),
                lSet.LCompassSetSection))
            .ToArray();

    public IReadOnlyList<int> LCompassSectionRead() =>
        LCompassGroupsRead()
            .Select((lGroup, lIndex) => (lGroup, lIndex))
            .Where(lPair => lPair.lGroup.LCompassGroupSection)
            .Select(lPair => lPair.lIndex)
            .ToArray();

    public static LCompassButton LCompassPlayRead(bool lPlaying) =>
        lPlaying
            ? LCompassButtonCreate(new LCompassEntry(LCompassPlayKey, "PCompassPause.svg", LCompassAccentNone), "Pause")
            : LCompassButtonCreate(LCompassPlayEntry, "Play");

    private static LCompassButton LCompassButtonCreate(LCompassEntry lEntry) =>
        LCompassButtonCreate(lEntry, lEntry.LCompassEntryKey);

    private static LCompassButton LCompassButtonCreate(LCompassEntry lEntry, string lText) =>
        new(
            lEntry.LCompassEntryKey,
            lEntry.LCompassEntryIcon,
            LLocalization.LLocalizationTextRead($"Compass.{lText}.Label"),
            LLocalization.LLocalizationTextRead($"Compass.{lText}.Tooltip"),
            lEntry.LCompassEntryAccent);

    public void LCompassRun(string lKey)
    {
        if (lKey != LCompassPlayKey)
        {
            lCompassFlow.LFlowShortcutRun(lKey);
            return;
        }

        if (LCompassPlaying)
        {
            lCompassFlow.LFlowPauseRaise();
        }
        else
        {
            lCompassFlow.LFlowPlayRaise();
        }
    }

    public void LCompassWaveformToggle() => lCompassFlow.LFlowWaveformSet(!lCompassFlow.LFlowWaveformActive);

    public void LCompassVolumeSet(double lRaw)
    {
        double lVolume = LPreferenceState.LPreferenceVolumeClamp(lRaw);
        if (lVolume == LCompassVolume)
        {
            return;
        }

        lCompassViewer.LViewerPlayback.LViewerVolumeSet(lVolume);
    }

    public static string LCompassVolumeFormat(double lRaw) =>
        Math.Round(LPreferenceState.LPreferenceVolumeClamp(lRaw)).ToString("0");

    public static double LCompassFillResolve(double lHostWidth, double lRaw)
    {
        if (lHostWidth <= 0)
        {
            return 0;
        }

        double lRate = (LPreferenceState.LPreferenceVolumeClamp(lRaw) - LCompassVolumeMinimum)
            / (LCompassVolumeMaximum - LCompassVolumeMinimum);
        return Math.Max(0, lHostWidth * lRate);
    }

    public static IReadOnlyList<double> LCompassSeparatorsResolve(IReadOnlyList<double> lGroupTops)
    {
        var lOpacities = new double[lGroupTops.Count];
        double lLineTop = double.NaN;
        for (int lIndex = 0; lIndex < lGroupTops.Count; lIndex++)
        {
            bool lLineStart = double.IsNaN(lLineTop) || lGroupTops[lIndex] > lLineTop;
            if (lLineStart)
            {
                lLineTop = lGroupTops[lIndex];
            }

            lOpacities[lIndex] = lLineStart ? 0 : 1;
        }

        return lOpacities;
    }
}
