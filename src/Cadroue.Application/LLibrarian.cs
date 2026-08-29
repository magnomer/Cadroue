using System;
using System.Collections.Generic;

using Cadroue.Core;

namespace Cadroue.Application;

public static class LLibrarian
{
    public static Func<string, LSidecarCoreRecord?>? LLibrarianCoreReader;
    public static Func<string, IReadOnlyList<long>>? LLibrarianKeyframesSeam;
    public static Func<string, LSidecarWaveformRecord?>? LLibrarianWaveformReader;
    public static Func<string, LSidecarEditRecord?>? LLibrarianEditReader;
    public static Func<string, LSidecarAudioRecord?>? LLibrarianAudioReader;
    public static Func<string, LSidecarSplitRecord?>? LLibrarianSplitReader;
    public static Func<string, LSidecarFixRecord?>? LLibrarianFixReader;
    public static Func<string, double>? LLibrarianLoudnessReader;
    public static Func<string, TimeSpan>? LLibrarianDurationReader;
    public static Func<string, TimeSpan>? LLibrarianDurationResolver;

    public static Func<string, bool>? LLibrarianFileChecker;
    public static Func<string, LSidecarSourceResult?>? LLibrarianSourceResolver;
    public static Func<string, string, bool>? LLibrarianSourceMatcher;

    public static Func<string, LSidecarEditRecord?, bool>? LLibrarianEditWriter;
    public static Func<string, LSidecarAudioRecord?, bool>? LLibrarianAudioWriter;
    public static Func<string, LSidecarSplitRecord?, bool>? LLibrarianSplitWriter;
    public static Func<string, LSidecarFixRecord?, bool>? LLibrarianFixWriter;
    public static Func<string, double, bool>? LLibrarianLoudnessWriter;
    public static Func<string, LSidecarWaveformRecord?, bool>? LLibrarianWaveformWriter;

    public static LSidecarCoreRecord? LLibrarianLoad(string lLibrarianSourcePath) =>
        LLibrarianCoreReader?.Invoke(lLibrarianSourcePath);

    public static IReadOnlyList<long> LLibrarianKeyframesLoad(string lLibrarianSourcePath) =>
        LLibrarianKeyframesSeam?.Invoke(lLibrarianSourcePath) ?? Array.Empty<long>();

    public static LSidecarWaveformRecord? LLibrarianWaveformLoad(string lLibrarianSourcePath) =>
        LLibrarianWaveformReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarEditRecord? LLibrarianEditLoad(string lLibrarianSourcePath) =>
        LLibrarianEditReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarAudioRecord? LLibrarianAudioLoad(string lLibrarianSourcePath) =>
        LLibrarianAudioReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarSplitRecord? LLibrarianSplitLoad(string lLibrarianSourcePath) =>
        LLibrarianSplitReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarFixRecord? LLibrarianFixLoad(string lLibrarianSourcePath) =>
        LLibrarianFixReader?.Invoke(lLibrarianSourcePath);

    public static double LLibrarianLoudnessRead(string lLibrarianSourcePath) =>
        LLibrarianLoudnessReader?.Invoke(lLibrarianSourcePath) ?? 0;

    public static TimeSpan LLibrarianDurationRead(string lLibrarianSourcePath) =>
        LLibrarianDurationReader?.Invoke(lLibrarianSourcePath) ?? TimeSpan.Zero;

    public static TimeSpan LLibrarianDurationResolve(string lLibrarianSourcePath) =>
        LLibrarianDurationResolver?.Invoke(lLibrarianSourcePath) ?? TimeSpan.Zero;

    public static bool LLibrarianFileCheck(string lLibrarianPath) =>
        LLibrarianFileChecker?.Invoke(lLibrarianPath) ?? false;

    public static LSidecarSourceResult? LLibrarianSourceResolve(string lLibrarianSidecarPath) =>
        LLibrarianSourceResolver?.Invoke(lLibrarianSidecarPath);

    public static bool LLibrarianSourceMatch(string lLibrarianMediaPath, string lLibrarianSidecarPath) =>
        LLibrarianSourceMatcher?.Invoke(lLibrarianMediaPath, lLibrarianSidecarPath) ?? false;

    public static bool LLibrarianEditSave(string lLibrarianSourcePath, LSidecarEditRecord? lLibrarianEdit) =>
        LLibrarianEditWriter?.Invoke(lLibrarianSourcePath, lLibrarianEdit) ?? false;

    public static bool LLibrarianAudioSave(string lLibrarianSourcePath, LSidecarAudioRecord? lLibrarianAudio) =>
        LLibrarianAudioWriter?.Invoke(lLibrarianSourcePath, lLibrarianAudio) ?? false;

    public static bool LLibrarianSplitSave(string lLibrarianSourcePath, LSidecarSplitRecord? lLibrarianSplit) =>
        LLibrarianSplitWriter?.Invoke(lLibrarianSourcePath, lLibrarianSplit) ?? false;

    public static bool LLibrarianFixSave(string lLibrarianSourcePath, LSidecarFixRecord? lLibrarianFix) =>
        LLibrarianFixWriter?.Invoke(lLibrarianSourcePath, lLibrarianFix) ?? false;

    public static bool LLibrarianLoudnessSave(string lLibrarianSourcePath, double lLibrarianLoudness) =>
        LLibrarianLoudnessWriter?.Invoke(lLibrarianSourcePath, lLibrarianLoudness) ?? false;

    public static bool LLibrarianWaveformSave(string lLibrarianSourcePath, LSidecarWaveformRecord? lLibrarianWaveform) =>
        LLibrarianWaveformWriter?.Invoke(lLibrarianSourcePath, lLibrarianWaveform) ?? false;
}
