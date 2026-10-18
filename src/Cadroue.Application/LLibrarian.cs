using System;
using System.Collections.Generic;

using Cadroue.Core;

namespace Cadroue.Application;

public static class LLibrarian
{
    public static Func<string, LSidecarCoreDocument?>? LLibrarianCoreReader;
    public static Func<string, LSidecarEditEntry?>? LLibrarianEditReader;
    public static Func<string, LSidecarAudioEntry?>? LLibrarianAudioReader;
    public static Func<string, LSidecarSplitEntry?>? LLibrarianSplitReader;
    public static Func<string, LSidecarFixEntry?>? LLibrarianFixReader;
    public static Func<string, IReadOnlyList<LSidecarDossier>?>? LLibrarianDiagnosisReader;
    public static Func<string, TimeSpan>? LLibrarianDurationReader;
    public static Func<string, Task<TimeSpan>>? LLibrarianDurationResolver;

    public static Func<string, bool>? LLibrarianFileChecker;
    public static Func<string, LSidecarSourceResult?>? LLibrarianSourceResolver;
    public static Func<string, string, bool>? LLibrarianSourceMatcher;

    public static Func<string, LSidecarEditEntry?, bool>? LLibrarianEditWriter;
    public static Func<string, LSidecarAudioEntry?, bool>? LLibrarianAudioWriter;
    public static Func<string, LSidecarSplitEntry?, bool>? LLibrarianSplitWriter;
    public static Func<string, LSidecarFixEntry?, bool>? LLibrarianFixWriter;
    public static Func<string, LKeyframeSourceIdentity, IReadOnlyCollection<LSidecarDossier>, bool>?
        LLibrarianDiagnosisWriter;

    public static LSidecarCoreDocument? LLibrarianLoad(string lLibrarianSourcePath) =>
        LLibrarianCoreReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarEditEntry? LLibrarianEditLoad(string lLibrarianSourcePath) =>
        LLibrarianEditReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarAudioEntry? LLibrarianAudioLoad(string lLibrarianSourcePath) =>
        LLibrarianAudioReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarSplitEntry? LLibrarianSplitLoad(string lLibrarianSourcePath) =>
        LLibrarianSplitReader?.Invoke(lLibrarianSourcePath);

    public static LSidecarFixEntry? LLibrarianFixLoad(string lLibrarianSourcePath) =>
        LLibrarianFixReader?.Invoke(lLibrarianSourcePath);

    public static IReadOnlyList<LSidecarDossier>? LLibrarianDiagnosisLoad(string lLibrarianSourcePath) =>
        LLibrarianDiagnosisReader?.Invoke(lLibrarianSourcePath);

    public static TimeSpan LLibrarianDurationRead(string lLibrarianSourcePath) =>
        LLibrarianDurationReader?.Invoke(lLibrarianSourcePath) ?? TimeSpan.Zero;

    public static Task<TimeSpan> LLibrarianDurationResolve(string lLibrarianSourcePath) =>
        LLibrarianDurationResolver?.Invoke(lLibrarianSourcePath) ?? Task.FromResult(TimeSpan.Zero);

    public static bool LLibrarianFileCheck(string lLibrarianPath) =>
        LLibrarianFileChecker?.Invoke(lLibrarianPath) ?? false;

    public static LSidecarSourceResult? LLibrarianSourceResolve(string lLibrarianSidecarPath) =>
        LLibrarianSourceResolver?.Invoke(lLibrarianSidecarPath);

    public static bool LLibrarianSourceMatch(string lLibrarianMediaPath, string lLibrarianSidecarPath) =>
        LLibrarianSourceMatcher?.Invoke(lLibrarianMediaPath, lLibrarianSidecarPath) ?? false;

    public static bool LLibrarianEditSave(string lLibrarianSourcePath, LSidecarEditEntry? lLibrarianEdit) =>
        LLibrarianEditWriter?.Invoke(lLibrarianSourcePath, lLibrarianEdit) ?? false;

    public static bool LLibrarianAudioSave(string lLibrarianSourcePath, LSidecarAudioEntry? lLibrarianAudio) =>
        LLibrarianAudioWriter?.Invoke(lLibrarianSourcePath, lLibrarianAudio) ?? false;

    public static bool LLibrarianSplitSave(string lLibrarianSourcePath, LSidecarSplitEntry? lLibrarianSplit) =>
        LLibrarianSplitWriter?.Invoke(lLibrarianSourcePath, lLibrarianSplit) ?? false;

    public static bool LLibrarianFixSave(string lLibrarianSourcePath, LSidecarFixEntry? lLibrarianFix) =>
        LLibrarianFixWriter?.Invoke(lLibrarianSourcePath, lLibrarianFix) ?? false;

    public static bool LLibrarianDiagnosisSave(
        string lLibrarianSourcePath,
        LKeyframeSourceIdentity lLibrarianIdentity,
        IReadOnlyCollection<LSidecarDossier> lLibrarianDossiers) =>
        LLibrarianDiagnosisWriter?.Invoke(lLibrarianSourcePath, lLibrarianIdentity, lLibrarianDossiers) ?? false;
}
