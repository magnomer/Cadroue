using Cadroue.Conduct;
using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsTimeline
{
    private readonly CSOptions csOptions;
    private readonly QSOptionsField qsOptionsField = new();
    private Func<CSOptionsTimeline>? qsOptionsTimelineValue;

    public QSOptionsTimeline(CSOptions options)
    {
        csOptions = options;
    }

    public FrameworkElement QSOptionsTimelineAttach(
        FrameworkElement surface,
        FrameworkElement palette,
        Func<string> spectrum,
        QSOptionsMode mode,
        Func<Border, string> choice)
    {
        QSOptionsTimelineDetach();
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        QContract.QContractFind<TextBlock>(surface, "PSTimelineOrder").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Order");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineStrip").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.StripOrder");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineSpacing").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeSpacing");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineMinimum").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Minimum");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineDelay").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeDelay");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineWait").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeDelayLabel");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineNotice").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeDelayNotice");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineOverlapping").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Overlapping");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineSections").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlappingSections");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineWaveform").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Waveform");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineDisplay").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.ShowWaveforms");
        QContract.QContractFind<TextBlock>(surface, "PSTimelineCaution").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformNotice");
        QContract.QContractFind<TextBlock>(surface, "PSTimelinePalette").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.SectionPalette");

        ContentControl arrangement = QContract.QContractFind<ContentControl>(surface, "PSTimelineArrangement");
        arrangement.Content = mode(
            snapshot.CSOptionsOrder,
            () => { },
            out _,
            new("MapFirst", "Options.Timeline.MapTop"),
            new("ViewfinderFirst", "Options.Timeline.ViewfinderTop"));
        Slider distance = QContract.QContractFind<Slider>(surface, "PSTimelineDistance");
        QSOptionsField.QSOptionsFieldPrepare(distance, snapshot.CSOptionsKeyframe);
        qsOptionsField.QSOptionsFieldAttach(
            distance,
            QContract.QContractFind<TextBlock>(surface, "PSTimelinePixels"),
            " px");
        Slider duration = QContract.QContractFind<Slider>(surface, "PSTimelineDuration");
        QSOptionsField.QSOptionsFieldPrepare(duration, snapshot.CSOptionsDelay);
        qsOptionsField.QSOptionsFieldAttach(
            duration,
            QContract.QContractFind<TextBlock>(surface, "PSTimelineMilliseconds"),
            " ms");
        CheckBox overlap = QContract.QContractFind<CheckBox>(surface, "PSTimelineOverlap");
        QSOptionsField.QSOptionsFieldPrepare(
            overlap,
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlapCheck"),
            snapshot.CSOptionsOverlap);
        CheckBox trace = QContract.QContractFind<CheckBox>(surface, "PSTimelineTrace");
        QSOptionsField.QSOptionsFieldPrepare(
            trace,
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformCheck"),
            snapshot.CSOptionsWaveform);
        QContract.QContractFind<ContentControl>(surface, "PSTimelineEditor").Content = palette;
        qsOptionsTimelineValue = () => new CSOptionsTimeline(
            choice((Border)arrangement.Content),
            distance.Value,
            duration.Value,
            spectrum(),
            QSOptionsField.QSOptionsFieldRead(overlap),
            QSOptionsField.QSOptionsFieldRead(trace));
        return surface;
    }

    public void QSOptionsTimelineDetach() => qsOptionsField.QSOptionsFieldDetach();

    public CSOptionsTimeline QSOptionsTimelineRead() => qsOptionsTimelineValue!();
}
