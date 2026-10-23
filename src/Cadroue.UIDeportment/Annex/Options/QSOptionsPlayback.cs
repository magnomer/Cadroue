using Cadroue.Conduct;
using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsPlayback
{
    private readonly CSOptions csOptions;
    private readonly QSMpv qsMpv;
    private readonly QSOptionsField qsOptionsField = new();
    private Action<bool>? qsOptionsPlaybackMpv;
    private Func<CSOptionsPlayback>? qsOptionsPlaybackValue;

    public QSOptionsPlayback(CSOptions options, QSMpv mpv)
    {
        csOptions = options;
        qsMpv = mpv;
    }

    public FrameworkElement QSOptionsPlaybackAttach(
        FrameworkElement surface,
        FrameworkElement flyleaf,
        FrameworkElement mpv,
        QSOptionsMode mode,
        Func<Border, string> choice)
    {
        QSOptionsPlaybackDetach();
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackPreview").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Preview");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackEngine").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Engine");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackNotice").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.MpvEditNotice");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackAutoplay").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Autoplay");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackOpening").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Autoplay");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackVolume").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.VolumePlate");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackSharing").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.VolumeMode");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackLoudness").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.DefaultVolume");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackMousewheel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Mousewheel");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackHover").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.OverTimeline");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackDragging").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.Dragging");
        QContract.QContractFind<TextBlock>(surface, "PSPlaybackGesture").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.WhileDragging");

        ContentControl selector = QContract.QContractFind<ContentControl>(surface, "PSPlaybackSelector");
        selector.Content = mode(
            ((LOptionsPort)csOptions).CSOptionsEngineRead()(qsMpv.QSMpvEnabled),
            () => { },
            out Action<string, bool> enable,
            new("Flyleaf", "Options.Playback.EngineFlyleaf"),
            new("Mpv", "Options.Playback.EngineMpv"));
        qsOptionsPlaybackMpv = enabled => enable("Mpv", enabled);
        enable("Mpv", qsMpv.QSMpvEnabled);
        qsMpv.QSMpvChanged += qsOptionsPlaybackMpv;
        _ = qsMpv.QSMpvUpdate();
        QContract.QContractFind<ContentControl>(surface, "PSPlaybackPrimary").Content = flyleaf;
        QContract.QContractFind<ContentControl>(surface, "PSPlaybackAlternate").Content = mpv;

        CheckBox automatic = QContract.QContractFind<CheckBox>(surface, "PSPlaybackAutomatic");
        QSOptionsField.QSOptionsFieldPrepare(
            automatic,
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.AutoplayCheck"),
            snapshot.CSOptionsAutoplay);
        ContentControl scope = QContract.QContractFind<ContentControl>(surface, "PSPlaybackScope");
        scope.Content =
            mode(
                snapshot.CSOptionsVolumeMode,
                () => { },
                out _,
                new("Unified", "Options.Playback.VolumeUnified"),
                new("PerTab", "Options.Playback.VolumePerTab"));
        Slider volume = QContract.QContractFind<Slider>(surface, "PSPlaybackSlider");
        QSOptionsField.QSOptionsFieldPrepare(volume, snapshot.CSOptionsVolume);
        qsOptionsField.QSOptionsFieldAttach(
            volume,
            QContract.QContractFind<TextBlock>(surface, "PSPlaybackReadout"),
            "%");
        ContentControl wheel = QContract.QContractFind<ContentControl>(surface, "PSPlaybackWheel");
        wheel.Content =
            mode(
                snapshot.CSOptionsWheel,
                () => { },
                out _,
                new("Seek", "Options.Playback.Seek"),
                new("Zoom", "Options.Playback.Zoom"),
                new("Volume", "Options.Playback.Volume"));
        CheckBox paused = QContract.QContractFind<CheckBox>(surface, "PSPlaybackPaused");
        QSOptionsField.QSOptionsFieldPrepare(
            paused,
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.DragPause"),
            snapshot.CSOptionsDrag);
        qsOptionsPlaybackValue = () => new CSOptionsPlayback(
            QSOptionsField.QSOptionsFieldRead(automatic),
            choice((Border)scope.Content),
            volume.Value,
            choice((Border)wheel.Content),
            QSOptionsField.QSOptionsFieldRead(paused),
            choice((Border)selector.Content));
        return surface;
    }

    public void QSOptionsPlaybackDetach()
    {
        qsMpv.QSMpvChanged -= qsOptionsPlaybackMpv;
        qsOptionsField.QSOptionsFieldDetach();
    }

    public CSOptionsPlayback QSOptionsPlaybackRead() => qsOptionsPlaybackValue!();
}
