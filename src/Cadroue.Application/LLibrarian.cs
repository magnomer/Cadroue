using System;
using System.Collections.Generic;

using Cadroue.Core;

namespace Cadroue.Application;

public static class LLibrarian
{
    public static Func<string, LSidecarCoreRecord?>? LLibrarianCoreLoadSeam;
    public static Func<string, IReadOnlyList<long>>? LLibrarianKeyframesSeam;
    public static Func<string, LSidecarWaveformRecord?>? LLibrarianWaveformLoadSeam;
    public static Func<string, LSidecarEditRecord?>? LLibrarianEditLoadSeam;
    public static Func<string, LSidecarAudioRecord?>? LLibrarianAudioLoadSeam;
    public static Func<string, double>? LLibrarianLoudnessLoadSeam;
    public static Func<string, TimeSpan>? LLibrarianDurationReadSeam;
    public static Func<string, TimeSpan>? LLibrarianDurationResolveSeam;

    public static Func<string, LSidecarEditRecord?, bool>? LLibrarianEditSaveSeam;
    public static Func<string, LSidecarAudioRecord?, bool>? LLibrarianAudioSaveSeam;
    public static Func<string, double, bool>? LLibrarianLoudnessSaveSeam;
    public static Func<string, LSidecarWaveformRecord?, bool>? LLibrarianWaveformSaveSeam;

    public static LSidecarCoreRecord? LLibrarianLoad(string lLibrarianSourcePath) =>
        LLibrarianCoreLoadSeam?.Invoke(lLibrarianSourcePath);

    public static IReadOnlyList<long> LLibrarianKeyframesLoad(string lLibrarianSourcePath) =>
        LLibrarianKeyframesSeam?.Invoke(lLibrarianSourcePath) ?? Array.Empty<long>();

    public static LSidecarWaveformRecord? LLibrarianWaveformLoad(string lLibrarianSourcePath) =>
        LLibrarianWaveformLoadSeam?.Invoke(lLibrarianSourcePath);

    public static LSidecarEditRecord? LLibrarianEditLoad(string lLibrarianSourcePath) =>
        LLibrarianEditLoadSeam?.Invoke(lLibrarianSourcePath);

    public static LSidecarAudioRecord? LLibrarianAudioLoad(string lLibrarianSourcePath) =>
        LLibrarianAudioLoadSeam?.Invoke(lLibrarianSourcePath);

    public static double LLibrarianLoudnessRead(string lLibrarianSourcePath) =>
        LLibrarianLoudnessLoadSeam?.Invoke(lLibrarianSourcePath) ?? 0;

    public static TimeSpan LLibrarianDurationRead(string lLibrarianSourcePath) =>
        LLibrarianDurationReadSeam?.Invoke(lLibrarianSourcePath) ?? TimeSpan.Zero;

    public static TimeSpan LLibrarianDurationResolve(string lLibrarianSourcePath) =>
        LLibrarianDurationResolveSeam?.Invoke(lLibrarianSourcePath) ?? TimeSpan.Zero;

    public static bool LLibrarianEditSave(string lLibrarianSourcePath, LSidecarEditRecord? lLibrarianEdit) =>
        LLibrarianEditSaveSeam?.Invoke(lLibrarianSourcePath, lLibrarianEdit) ?? false;

    public static bool LLibrarianAudioSave(string lLibrarianSourcePath, LSidecarAudioRecord? lLibrarianAudio) =>
        LLibrarianAudioSaveSeam?.Invoke(lLibrarianSourcePath, lLibrarianAudio) ?? false;

    public static bool LLibrarianLoudnessSave(string lLibrarianSourcePath, double lLibrarianLoudness) =>
        LLibrarianLoudnessSaveSeam?.Invoke(lLibrarianSourcePath, lLibrarianLoudness) ?? false;

    public static bool LLibrarianWaveformSave(string lLibrarianSourcePath, LSidecarWaveformRecord? lLibrarianWaveform) =>
        LLibrarianWaveformSaveSeam?.Invoke(lLibrarianSourcePath, lLibrarianWaveform) ?? false;
}
