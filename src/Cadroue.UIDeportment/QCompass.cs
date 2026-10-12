using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QCompassButton(
    string QCompassButtonKey,
    string QCompassButtonIcon,
    string QCompassButtonLabel,
    string QCompassButtonTooltip,
    string QCompassButtonAccent);

public sealed record QCompassGroup(IReadOnlyList<QCompassButton> QCompassGroupButtons, bool QCompassGroupSection);

public sealed class QCompass
{
    private sealed record QCompassEntry(string QCompassEntryKey, string QCompassEntryIcon, string QCompassEntryAccent);

    private sealed record QCompassCluster(IReadOnlyList<QCompassEntry> QCompassSetEntries, bool QCompassSetSection);

    public const string QCompassPlayKey = "Play";
    public const string QCompassAccentPositive = "Positive";
    public const string QCompassAccentNegative = "Negative";
    public const string QCompassAccentNone = "";
    public const double QCompassVolumeMinimum = 0;
    public const double QCompassVolumeMaximum = 100;

    private static readonly QCompassEntry QCompassPlayEntry =
        new(QCompassPlayKey, "PCompassPlay.svg", QCompassAccentPositive);

    private static readonly IReadOnlyList<QCompassCluster> QCompassSets =
    [
        new(
            [
                new("ZoomIn", "PCompassZoomIncrease.svg", QCompassAccentNone),
                new("ZoomOut", "PCompassZoomDecrease.svg", QCompassAccentNone),
            ],
            false),
        new([QCompassPlayEntry], false),
        new(
            [
                new("SectionAdd", "PCompassSectionAdd.svg", QCompassAccentNone),
                new("SectionDelete", "PCompassRemove.svg", QCompassAccentNegative),
            ],
            true),
        new(
            [
                new("SectionStart", "PCompassStart.svg", QCompassAccentNone),
                new("SectionSplit", "PCompassSplit.svg", QCompassAccentNone),
                new("SectionEnd", "PCompassEnd.svg", QCompassAccentNone),
            ],
            true),
        new(
            [
                new("KeyframePrevious", "PCompassKeyframePrevious.svg", QCompassAccentNone),
                new("KeyframeNearest", "PCompassKeyframeNear.svg", QCompassAccentNone),
                new("KeyframeNext", "PCompassKeyframeNext.svg", QCompassAccentNone),
            ],
            false),
    ];

    private readonly QFlow qCompassFlow;
    private readonly QViewer qCompassViewer;
    private readonly bool qCompassSectionShown;

    public QCompass(QFlow lFlow, QViewer lViewer, bool lSectionShown)
    {
        qCompassFlow = lFlow;
        qCompassViewer = lViewer;
        qCompassSectionShown = lSectionShown;
    }

    public bool QCompassPlaying => qCompassViewer.QViewerPlaying;

    public double QCompassVolume => qCompassViewer.QViewerVolume;

    public bool QCompassWaveformActive => qCompassFlow.QFlowWaveformActive;

    public bool QCompassEditActive => qCompassFlow.QFlowEdit.QFlowSectionEditable;

    public IReadOnlyList<QCompassGroup> QCompassGroupsRead() =>
        QCompassSets
            .Where(lSet => qCompassSectionShown || !lSet.QCompassSetSection)
            .Select(lSet => new QCompassGroup(
                lSet.QCompassSetEntries.Select(QCompassButtonCreate).ToArray(),
                lSet.QCompassSetSection))
            .ToArray();

    public IReadOnlyList<int> QCompassSectionRead() =>
        QCompassGroupsRead()
            .Select((lGroup, lIndex) => (lGroup, lIndex))
            .Where(lPair => lPair.lGroup.QCompassGroupSection)
            .Select(lPair => lPair.lIndex)
            .ToArray();

    public static QCompassButton QCompassPlayRead(bool lPlaying) =>
        lPlaying
            ? QCompassButtonCreate(new QCompassEntry(QCompassPlayKey, "PCompassPause.svg", QCompassAccentNone), "Pause")
            : QCompassButtonCreate(QCompassPlayEntry, "Play");

    private static QCompassButton QCompassButtonCreate(QCompassEntry lEntry) =>
        QCompassButtonCreate(lEntry, lEntry.QCompassEntryKey);

    private static QCompassButton QCompassButtonCreate(QCompassEntry lEntry, string lText) =>
        new(
            lEntry.QCompassEntryKey,
            lEntry.QCompassEntryIcon,
            LLocalization.LLocalizationTextRead($"Compass.{lText}.Label"),
            LLocalization.LLocalizationTextRead($"Compass.{lText}.Tooltip"),
            lEntry.QCompassEntryAccent);

    public void QCompassRun(string lKey)
    {
        if (lKey != QCompassPlayKey)
        {
            qCompassFlow.QFlowShortcutRun(lKey);
            return;
        }

        if (QCompassPlaying)
        {
            qCompassFlow.QFlowPauseRaise();
        }
        else
        {
            qCompassFlow.QFlowPlayRaise();
        }
    }

    public void QCompassWaveformToggle() => qCompassFlow.QFlowWaveformSet(!qCompassFlow.QFlowWaveformActive);

    public void QCompassVolumeSet(double lRaw)
    {
        double lVolume = LPreferenceState.LPreferenceVolumeClamp(lRaw);
        if (lVolume == QCompassVolume)
        {
            return;
        }

        qCompassViewer.QViewerPlayback.QViewerVolumeSet(lVolume);
    }

    public static string QCompassVolumeFormat(double lRaw) =>
        Math.Round(LPreferenceState.LPreferenceVolumeClamp(lRaw)).ToString("0");

    public static double QCompassFillResolve(double lHostWidth, double lRaw)
    {
        if (lHostWidth <= 0)
        {
            return 0;
        }

        double lRate = (LPreferenceState.LPreferenceVolumeClamp(lRaw) - QCompassVolumeMinimum)
            / (QCompassVolumeMaximum - QCompassVolumeMinimum);
        return Math.Max(0, lHostWidth * lRate);
    }

    public static IReadOnlyList<double> QCompassSeparatorsResolve(IReadOnlyList<double> lGroupTops)
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
