using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static LPreferenceState TPreferenceCurrentRead() => LPreference.LPreferenceStateCurrent;
    internal static bool TPreferenceRestore(LPreferenceState state) => LPreference.LPreferenceStateSet(state);
    internal static void TPreferenceSaveAttach(Func<LPreferenceState, bool>? seam) =>
        LPreference.LPreferenceSaveSeam = seam;

    internal static QSOptions TOptionsCreate(LPreferenceState draft, bool mpv) =>
        new(TAtelierCreate().CAtelierUsher, draft, mpv);
    internal static void TOptionsMpvAttach(QSOptions options, Action<bool> handler) =>
        options.QSOptionsMpv.QSOptionsMpvChanged += handler;
    internal static void TOptionsMpvSet(QSOptions options, bool enabled) =>
        options.QSOptionsMpv.QSOptionsMpvSet(enabled);
    internal static string TOptionsEngineRead(QSOptions options) => options.QSOptionsEngineRead();
    internal static void TOptionsPageSelect(QSOptions options, QSOptionsPage page) =>
        options.QSOptionsPageSelect(page);
    internal static bool TOptionsApply(QSOptions options) => options.QSOptionsApply();
    internal static bool TOptionsRecordCheck(QSOptions options) => options.QSOptionsRecordCheck();
    internal static string TOptionsFfmpegResolve(string folder) => QSOptions.QSOptionsFfmpegResolve(folder);

    internal static QSSpectrum TSpectrumCreate(string name, string folder) => new(name, () => folder);
    internal static void TSpectrumAttach(QSSpectrum spectrum, Action handler) => spectrum.QSSpectrumChanged += handler;
    internal static void TSpectrumNamesSet(QSSpectrum spectrum, IReadOnlyList<string> names) =>
        spectrum.QSSpectrumNamesSet(names);
    internal static void TSpectrumSelect(QSSpectrum spectrum, string name) => spectrum.QSSpectrumSelect(name);
    internal static bool TSpectrumRemove(QSSpectrum spectrum, string name, bool native, string? path) =>
        spectrum.QSSpectrumRemove(name, native, path);

    internal static void TPresetSelectionClose(LPresetSelection selection) => selection.LPresetSelectionClose();
    internal static bool TPresetSelectionCommit(LPresetSelection selection, string oldName, string newName) =>
        selection.LPresetSelectionCommit(oldName, newName);
    internal static LPresetDocument TPresetDocumentCreate(string name, string display) =>
        new() { LPresetName = name, LPresetDisplay = display };
    internal static QSEncoder TEncoderCreate(LPresetSelection selection, bool smart) =>
        TExportCreate(selection, smart ? "Split" : "Convert").QExportEncoderCreate();
    internal static void TEncoderVideoAttach(QSEncoder encoder, Action handler) =>
        encoder.QSEncoderVideo.QSEncoderVideoChanged += handler;
    internal static void TEncoderSizeAttach(QSEncoder encoder, Action handler) =>
        encoder.QSEncoderSize.QSEncoderSizeChanged += handler;
    internal static bool TEncoderVideoSet(QSEncoder encoder, string codec, string rate) =>
        encoder.QSEncoderVideo.QSEncoderVideoSet(codec, rate);
    internal static bool TEncoderAudioSet(QSEncoder encoder, string codec, string rate) =>
        encoder.QSEncoderAudio.QSEncoderAudioSet(codec, rate);
    internal static bool TEncoderTierSelect(QSEncoder encoder, int tier) =>
        encoder.QSEncoderSize.QSEncoderTierSelect(tier);
    internal static bool TEncoderSizeSet(QSEncoder encoder, int width, int height) =>
        encoder.QSEncoderSize.QSEncoderSizeSet(
            width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture));
    internal static string TEncoderSizeFormat(QSEncoder encoder) =>
        encoder.QSEncoderSize.QSEncoderSizeValue.CSEncoderDimensionFormat();
    internal static string TEncoderSuffixSelect(QSEncoder encoder, string policy, string shown) =>
        encoder.QSEncoderSuffix.QSEncoderSuffixSelect(policy, shown);
    internal static string TEncoderLocationSelect(QSEncoder encoder, string mode, string shown) =>
        encoder.QSEncoderLocation.QSEncoderLocationSelect(mode, shown);
    internal static string TEncoderNoticeResolve(QSEncoder encoder, int facet)
    {
        encoder.QSEncoderVideo.QSEncoderModeSet(facet);
        return encoder.QSEncoderVideo.QSEncoderNoticeResolve();
    }
    internal static void TEncoderApply(QSEncoder encoder) => encoder.QSEncoderApply();
    internal static void TEncoderClose(QSEncoder encoder) => encoder.QSEncoderClose();

    internal static QSKeymap TKeymapCreate(List<LBindingEntry>? records) => new(records);
    internal static void TKeymapAttach(QSKeymap keymap, Action<QSKeymapChord> handler) =>
        keymap.QSKeymapChordChanged += handler;
    internal static QSKeymapChord TKeymapChordRead(QSKeymap keymap, string token) => keymap.QSKeymapChordRead(token);
    internal static void TKeymapChordStart(QSKeymap keymap, QSKeymapChord chord) => keymap.QSKeymapChordStart(chord);
    internal static void TKeymapPendingSet(QSKeymap keymap, QSKeymapChord chord, string gesture) =>
        keymap.QSKeymapPendingSet(chord, gesture);
    internal static void TKeymapChordCommit(QSKeymap keymap, QSKeymapChord chord) => keymap.QSKeymapChordCommit(chord);
    internal static void TKeymapChordCancel(QSKeymap keymap, QSKeymapChord chord) => keymap.QSKeymapChordCancel(chord);
    internal static void TKeymapDefaultApply(QSKeymap keymap) => keymap.QSKeymapDefaultApply();
    internal static List<LBindingEntry> TKeymapRecordsRead(QSKeymap keymap) => keymap.QSKeymapRecordsRead();
    internal static string TBindingGestureFormat(string key, bool ctrl, bool alt, bool shift, bool win) =>
        LBinding.LBindingGestureFormat(key, ctrl, alt, shift, win);
    internal static string TBindingDefaultRead(string token) => LBindingCatalog.LBindingDefaultRead(token);
    internal static IReadOnlyList<LBindingCommand> TBindingCatalogRead() => LBindingCatalog.LBindingCatalogRead();

    internal static QSMonitor TMonitorCreate() => new();
    internal static void TMonitorCursorAttach(QSMonitor monitor, Action<TimeSpan> handler) =>
        monitor.QSMonitorCursorChanged += handler;
    internal static void TMonitorZoomAttach(QSMonitor monitor, Action handler) =>
        monitor.QSMonitorViewport.QSMonitorZoomChanged += handler;
    internal static void TMonitorCursorSet(QSMonitor monitor, TimeSpan cursor) => monitor.QSMonitorCursorSet(cursor);
    internal static void TMonitorZoom(QSMonitor monitor, double factor, double most) =>
        monitor.QSMonitorViewport.QSMonitorZoom(factor, most);
    internal static void TMonitorOffsetSet(QSMonitor monitor, double offset) =>
        monitor.QSMonitorViewport.QSMonitorOffsetSet(offset);
    internal static double TMonitorFractionResolve(QSMonitor monitor, double local) =>
        monitor.QSMonitorViewport.QSMonitorFractionResolve(local);
    internal static double TMonitorLocalResolve(QSMonitor monitor, double fraction) =>
        monitor.QSMonitorViewport.QSMonitorLocalResolve(fraction);
    internal static double TMonitorColumnRead(QSMonitor monitor, double[] envelope, int column, int columns) =>
        monitor.QSMonitorViewport.QSMonitorColumnRead(envelope, column, columns);
    internal static void TMonitorIncreaseZoom(QSMonitor monitor) => monitor.QSMonitorViewport.QSMonitorIncreaseZoom();
    internal static void TMonitorDecreaseZoom(QSMonitor monitor) => monitor.QSMonitorViewport.QSMonitorDecreaseZoom();
    internal static QSMonitorScrollbar TMonitorScrollRead(QSMonitor monitor) =>
        monitor.QSMonitorViewport.QSMonitorScrollRead();
    internal static QSMonitorHead TMonitorHeadResolve(QSMonitor monitor, double width, TimeSpan duration) =>
        monitor.QSMonitorHeadResolve(width, duration);
    internal static void TMonitorSeekAttach(QSMonitor monitor, Action<TimeSpan> handler) =>
        monitor.QSMonitorSeekApplied += handler;
    internal static void TMonitorSeekHandle(
        QSMonitor monitor, bool pressed, double x, double width, TimeSpan duration) =>
        monitor.QSMonitorSeekHandle(pressed, x, width, duration);
    internal static void TMonitorPlayAttach(QSMonitor monitor, Action play, Action pause)
    {
        monitor.QSMonitorPlayApplied += play;
        monitor.QSMonitorPauseApplied += pause;
    }

    internal static void TMonitorPlayingSet(QSMonitor monitor, bool playing) => monitor.QSMonitorPlayingSet(playing);
    internal static void TMonitorPlayHandle(QSMonitor monitor) => monitor.QSMonitorPlayHandle();
    internal static QSMonitorFace TMonitorFaceRead(QSMonitor monitor) => monitor.QSMonitorFaceRead();
    internal static void TMonitorBypassAttach(QSMonitor monitor, Action<bool> handler) =>
        monitor.QSMonitorBypassApplied += handler;
    internal static void TMonitorBypassSet(QSMonitor monitor, bool bypass) => monitor.QSMonitorBypassSet(bypass);
    internal static void TMonitorRadioHandle(QSMonitor monitor, bool bypass) => monitor.QSMonitorRadioHandle(bypass);
    internal static string TMonitorStatusRead(QSMonitor monitor, bool after) =>
        monitor.QSMonitorWaveform.QSMonitorStatusRead(after);
    internal static QSMonitorFrame TMonitorFrameResolve(QSMonitor monitor, bool after, double width, double height) =>
        monitor.QSMonitorFrameResolve(after, width, height);
    internal static QFlowWaveformOutline TMonitorOutlineResolve(
        double[] envelope, double width, double height, double scale, double offset) =>
        QSMonitorPlan.QSMonitorOutlineResolve(envelope, width, height, scale, offset);

    internal static QSVerdict TVerdictCreate(string title, IReadOnlyList<QSVerdictRow> rows) => new(title, rows);
    internal static bool TVerdictDetailCheck(QSVerdictRow row) => QSVerdict.QSVerdictDetailCheck(row);

    internal static QSAbout TAboutCreate() => new(TAtelierCreate().CAtelierUsher);
    internal static bool TAboutTapChange(QSAbout about, DateTime now) => about.QSAboutTap.QSAboutTapChange(now);
}
