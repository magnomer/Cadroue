using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LPreferenceState TPreferenceCurrentRead() => LPreference.LPreferenceStateCurrent;
    internal static bool TPreferenceRestore(LPreferenceState state) => LPreference.LPreferenceStateSet(state);
    internal static void TPreferenceSaveAttach(Func<LPreferenceState, bool>? seam) =>
        LPreference.LPreferenceSaveSeam = seam;

    internal static LSOptions TOptionsCreate(LPreferenceState draft, bool mpv) => new(draft, mpv);
    internal static void TOptionsMpvAttach(LSOptions options, Action<bool> handler) =>
        options.LSOptionsMpvChange += handler;
    internal static void TOptionsMpvSet(LSOptions options, bool enabled) => options.LSOptionsMpvSet(enabled);
    internal static string TOptionsEngineRead(LSOptions options) => options.LSOptionsEngineRead();
    internal static void TOptionsPageSelect(LSOptions options, LSOptionsPage page) =>
        options.LSOptionsPageSelect(page);
    internal static bool TOptionsApply(LSOptions options) => options.LSOptionsApply();
    internal static bool TOptionsRecordCheck(LSOptions options) => options.LSOptionsRecordCheck();
    internal static string TOptionsFfmpegResolve(string folder) => LSOptions.LSOptionsFfmpegResolve(folder);

    internal static LSSpectrum TSpectrumCreate(string name, string folder) => new(name, () => folder);
    internal static void TSpectrumAttach(LSSpectrum spectrum, Action handler) => spectrum.LSSpectrumChange += handler;
    internal static void TSpectrumNamesSet(LSSpectrum spectrum, IReadOnlyList<string> names) =>
        spectrum.LSSpectrumNamesSet(names);
    internal static void TSpectrumSelect(LSSpectrum spectrum, string name) => spectrum.LSSpectrumSelect(name);
    internal static bool TSpectrumRemove(LSSpectrum spectrum, string name, bool native, string? path) =>
        spectrum.LSSpectrumRemove(name, native, path);

    internal static LSEncoder TEncoderCreate(LPreset source, bool smart) => new(source, smart);
    internal static void TEncoderVideoAttach(LSEncoder encoder, Action handler) =>
        encoder.LSEncoderVideoChange += handler;
    internal static void TEncoderSizeAttach(LSEncoder encoder, Action handler) =>
        encoder.LSEncoderSizeChange += handler;
    internal static bool TEncoderVideoSet(LSEncoder encoder, string codec, string rate) =>
        encoder.LSEncoderVideoSet(codec, rate);
    internal static bool TEncoderAudioSet(LSEncoder encoder, string codec, string rate) =>
        encoder.LSEncoderAudioSet(codec, rate);
    internal static bool TEncoderTierSelect(LSEncoder encoder, int tier) => encoder.LSEncoderTierSelect(tier);
    internal static bool TEncoderSizeSet(LSEncoder encoder, int width, int height) =>
        encoder.LSEncoderSizeSet(width, height);
    internal static string TEncoderSizeFormat(LSEncoder encoder) => encoder.LSEncoderSizeFormat();
    internal static string TEncoderSuffixSelect(LSEncoder encoder, string policy, string shown) =>
        encoder.LSEncoderSuffixSelect(policy, shown);
    internal static string TEncoderLocationSelect(LSEncoder encoder, string mode, string shown) =>
        encoder.LSEncoderLocationSelect(mode, shown);
    internal static string TEncoderNoticeResolve(LSEncoder encoder, string mode) =>
        encoder.LSEncoderNoticeResolve(mode);
    internal static void TEncoderApply(LSEncoder encoder) => encoder.LSEncoderApply();
    internal static LPreset TPresetCreate() => new();
    internal static string TPresetSuffixRead(LPreset preset, string policy) => preset.LPresetSuffixRead(policy);

    internal static LSKeymap TKeymapCreate(List<LBindingRecord>? records) => new(records);
    internal static void TKeymapAttach(LSKeymap keymap, Action<LSKeymapChord> handler) =>
        keymap.LSKeymapChordChange += handler;
    internal static LSKeymapChord TKeymapChordRead(LSKeymap keymap, string token) => keymap.LSKeymapChordRead(token);
    internal static void TKeymapChordStart(LSKeymap keymap, LSKeymapChord chord) => keymap.LSKeymapChordStart(chord);
    internal static void TKeymapPendingSet(LSKeymap keymap, LSKeymapChord chord, string gesture) =>
        keymap.LSKeymapPendingSet(chord, gesture);
    internal static void TKeymapChordCommit(LSKeymap keymap, LSKeymapChord chord) => keymap.LSKeymapChordCommit(chord);
    internal static void TKeymapChordCancel(LSKeymap keymap, LSKeymapChord chord) => keymap.LSKeymapChordCancel(chord);
    internal static void TKeymapDefaultApply(LSKeymap keymap) => keymap.LSKeymapDefaultApply();
    internal static List<LBindingRecord> TKeymapRecordsRead(LSKeymap keymap) => keymap.LSKeymapRecordsRead();
    internal static string TBindingGestureFormat(string key, bool ctrl, bool alt, bool shift, bool win) =>
        LBinding.LBindingGestureFormat(key, ctrl, alt, shift, win);
    internal static string TBindingDefaultRead(string token) => LBinding.LBindingDefaultRead(token);
    internal static IReadOnlyList<LBindingCommand> TBindingCatalogRead() => LBinding.LBindingCatalogRead();

    internal static LSMonitor TMonitorCreate() => new();
    internal static void TMonitorCursorAttach(LSMonitor monitor, Action<TimeSpan> handler) =>
        monitor.LSMonitorCursorChange += handler;
    internal static void TMonitorZoomAttach(LSMonitor monitor, Action handler) =>
        monitor.LSMonitorZoomChange += handler;
    internal static void TMonitorCursorSet(LSMonitor monitor, TimeSpan cursor) => monitor.LSMonitorCursorSet(cursor);
    internal static void TMonitorZoom(LSMonitor monitor, double factor, double most) =>
        monitor.LSMonitorZoom(factor, most);
    internal static void TMonitorOffsetSet(LSMonitor monitor, double offset) => monitor.LSMonitorOffsetSet(offset);
    internal static double TMonitorFractionResolve(LSMonitor monitor, double local) =>
        monitor.LSMonitorFractionResolve(local);
    internal static double TMonitorLocalResolve(LSMonitor monitor, double fraction) =>
        monitor.LSMonitorLocalResolve(fraction);
    internal static double TMonitorColumnRead(LSMonitor monitor, double[] envelope, int column, int columns) =>
        monitor.LSMonitorColumnRead(envelope, column, columns);

    internal static LSVerdict TVerdictCreate(string title, IReadOnlyList<LSVerdictRow> rows) => new(title, rows);
    internal static bool TVerdictDetailCheck(LSVerdictRow row) => LSVerdict.LSVerdictDetailCheck(row);

    internal static LSAbout TAboutCreate() => new();
    internal static bool TAboutTapChange(LSAbout about, DateTime now) => about.LSAboutTapChange(now);
}
