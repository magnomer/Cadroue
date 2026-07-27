namespace Cadroue.Core;

/// <summary>
/// Snapshot of every export setting in force at the moment work was scheduled.
///
/// This is a value copy on purpose: the export panel keeps mutating its own state
/// after the user presses Add List, and a queued item must keep the settings it was
/// scheduled with. It is also UI-free so the schedule can live in the backend
/// without depending on the shell.
///
/// Every control in the export dialog must appear here. A setting the user can change
/// but that never reaches this record is a setting the encoder will silently ignore.
/// </summary>
public sealed record LWorkOutput(
    // Output
    string LWorkOutputNamePattern,
    string LWorkOutputContainer,
    string LWorkOutputExtension,
    string LWorkOutputLocation,
    string LWorkOutputLocationFolder,
    string LWorkOutputExportMode,
    // Video
    string LWorkOutputVideoStream,
    string LWorkOutputVideoMode,
    string LWorkOutputVideoEncoder,
    string LWorkOutputRateControl,
    string LWorkOutputQuality,
    string LWorkOutputSpeedPreset,
    string LWorkOutputVideoSize,
    string LWorkOutputVideoFps,
    string LWorkOutputPixelFormat,
    /// <summary>Per-encoder extra options, keyed by FFmpeg option (e.g. "-tune").</summary>
    IReadOnlyDictionary<string, string> LWorkOutputVideoExtras,
    // Audio
    string LWorkOutputAudioStream,
    string LWorkOutputAudioMode,
    string LWorkOutputAudioEncoder,
    string LWorkOutputAudioBitrate,
    string LWorkOutputAudioSampleRate,
    string LWorkOutputAudioChannels);
