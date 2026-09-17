using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static bool TClassifierMatch(LSceneFunnelRule rule, string name) =>
        LClassifier.LClassifierMatch(rule, name);

    internal static int TClassifierRouteRead(
        IReadOnlyList<LSceneFunnelRule> rules,
        string name,
        Func<int, bool>? usable = null) =>
        LClassifier.LClassifierRouteRead(rules, name, usable);

    internal static IReadOnlyList<LSeriesGroup> TSeriesResolve(
        IReadOnlyList<string> paths,
        bool strict,
        LSeriesNameMode nameMode = LSeriesNameMode.LSeriesNameBase) =>
        LSeries.LSeriesResolve(paths, strict, nameMode);

    internal static IReadOnlyList<LPiece> TPieceValidSelect(IReadOnlyList<LPiece> sections, TimeSpan duration) =>
        LPiece.LPieceValidSelect(sections, duration);

    internal static bool TPieceInsideCheck(
        IReadOnlyList<LPiece> sections, TimeSpan time, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceInsideCheck(sections, time, skipIndex, overlapAllowed);

    internal static TimeSpan TPieceLimitRead(
        IReadOnlyList<LPiece> sections, TimeSpan from, TimeSpan ceiling, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceLimitRead(sections, from, ceiling, skipIndex, overlapAllowed);

    internal static TimeSpan TPieceFloorRead(
        IReadOnlyList<LPiece> sections, TimeSpan until, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceFloorRead(sections, until, skipIndex, overlapAllowed);

    internal static LPieceResult? TPieceAdd(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, TimeSpan duration, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceAdd(sections, cursor, duration, colorIndex, overlapAllowed);

    internal static LPieceResult? TPieceEndCreate(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndCreate(sections, cursor, colorIndex, overlapAllowed);

    internal static LPieceResult? TPieceStartSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, TimeSpan duration,
        int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceOriginSet(sections, activeIndex, cursor, duration, colorIndex, overlapAllowed);

    internal static LPieceResult? TPieceEndSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndSet(sections, activeIndex, cursor, colorIndex, overlapAllowed);

    internal static bool TPieceIntersectCheck(
        IReadOnlyList<LPiece> sections, TimeSpan origin, TimeSpan end, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceIntersectCheck(sections, origin, end, skipIndex, overlapAllowed);

    internal static LSegment TSegmentCreate() => new();

    internal static void TSegmentSet(LSegment segment, IReadOnlyList<LPiece> sections, int? select) =>
        segment.LSegmentSet(sections, select);

    internal static bool TSegmentDelete(LSegment segment, IReadOnlyList<int> indexes, int approved) =>
        segment.LSegmentDelete(indexes, approved);

    internal static bool TSegmentClear(LSegment segment, int approved) => segment.LSegmentClear(approved);

    internal static int TSegmentVersionRead(LSegment segment) => segment.LSegmentVersionRead();

    internal static IReadOnlyList<LPiece> TSegmentListRead(LSegment segment) => segment.LSegmentListRead();

    internal static void TSegmentFaultAttach(LSegment segment, Action<LSegmentFault, int> handler) =>
        segment.LSegmentFaultNotice += handler;

    internal static bool TSegmentBoundSet(
        LSegment segment, IReadOnlyList<LPiece> sections, int? select, TimeSpan duration) =>
        segment.LSegmentBoundSet(sections, select, duration);

    internal static void TSegmentLoad(LSegment segment, string source, TimeSpan duration)
    {
        segment.LSegmentSourceSet(source);
        segment.LSegmentLoad(duration);
    }

    internal static void TSegmentSeamSet(Func<string, IReadOnlyList<LSidecarSectionRecord>>? seam) =>
        LSegment.LSegmentLoadSeam = seam;

    internal static LPiece TPieceCreate(LSidecarSectionRecord record) => LPiece.LPieceCreate(record);

    internal static LPieceDivision? TPieceDivide(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, int colorIndex) =>
        LPiece.LPieceDivide(sections, activeIndex, cursor, colorIndex);

    internal static LSpool TSpoolCreate(TimeSpan duration) => new(duration);
    internal static TimeSpan TSpoolStepResolve(LSpool spool, int count) => spool.LSpoolStepResolve(count);
    internal static void TSpoolZoom(LSpool spool, TimeSpan cursor, int steps) => spool.LSpoolZoom(cursor, steps);
    internal static void TSpoolStartSet(LSpool spool, TimeSpan start) => spool.LSpoolStartSet(start);
    internal static void TSpoolEndSet(LSpool spool, TimeSpan end) => spool.LSpoolEndSet(end);

    internal static IReadOnlyList<LKeyframeEntry> TKeyframeVisibleResolve(
        IReadOnlyList<LKeyframeEntry> keyframes, TimeSpan cursor, LSpool spool) =>
        LKeyframeView.LKeyframeVisibleResolve(keyframes, cursor, spool);

    internal static IReadOnlyList<LKeyframeScanRange> TKeyframeCoverageResolve(
        IReadOnlyList<LKeyframeScanRange> ranges, LSpool spool, bool wholeMedia) =>
        LKeyframeView.LKeyframeCoverageResolve(ranges, spool, wholeMedia);

    internal static LKeyframeMoveResult TKeyframeMoveResolve(
        IReadOnlyCollection<long> keyframes, IReadOnlySet<int> scannedSpans,
        TimeSpan duration, TimeSpan cursor, int direction, IReadOnlySet<int>? failedSpans = null) =>
        LKeyframeOrchestrator.LKeyframeMoveResolve(keyframes, scannedSpans, duration, cursor, direction, failedSpans);

    internal static LPreferenceState TPreferenceDefaultCreate() => LPreferenceState.LPreferenceDefaultCreate();
    internal static LPreferenceState TPreferenceCreate(int cleanupDays = 30) =>
        new() { LPreferenceCleanupDays = cleanupDays };
    internal static LPreferenceState TPreferenceClone(LPreferenceState state) => state.LPreferenceClone();
    internal static void TPreferenceNormalize(LPreferenceState state) => state.LPreferenceNormalize();
    internal static IEnumerable<string> TPreferenceDifferenceRead(LPreferenceState state, LPreferenceState before) =>
        state.LPreferenceDifferenceRead(before);
    internal static bool TPreferenceFoldedRead(
        LPreferenceState state,
        string groupName,
        bool fallback = true) =>
        state.LPreferenceFoldRead(groupName, fallback);
    internal static void TPreferenceFoldedSet(
        LPreferenceState state,
        string groupName,
        bool folded) =>
        state.LPreferenceFold[groupName] = folded;

    internal static LGroupSelection TGroupSelectionCreate(
        bool groupAuto = false,
        bool groupStrict = true,
        LSeriesNameMode nameMode = LSeriesNameMode.LSeriesNameBase) =>
        new(groupAuto, groupStrict, nameMode);

    internal static void TGroupModeRead(LGroupSelection selection, LSeriesNameMode mode) =>
        selection.LGroupModeChange(mode);

    internal static IReadOnlyList<LSeriesGroup> TGroupResolve(LGroupSelection selection, IReadOnlyList<string> paths) =>
        selection.LGroupResolve(paths);

    internal static LBridgePlan TBridgeResolve(
        IReadOnlyList<TimeSpan> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static LBridgePlan TBridgeResolve(
        IReadOnlyList<LKeyframeEntry> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static bool TBridgeEndCheck(TimeSpan end, TimeSpan duration, double framerate) =>
        LBridge.LBridgeEndCheck(end, duration, framerate);
    internal static bool TBridgeLeadingNormalize(byte[] bytes) => LBridge.LBridgeLeadingNormalize(bytes);

    internal static bool TRetentionExpiredCheck(DateTime writeUtc, DateTime nowUtc, int days) =>
        LRetention.LRetentionExpiredCheck(writeUtc, nowUtc, days);
    internal static bool TRetentionSweptCheck(string relativePath) =>
        LRetention.LRetentionSweptCheck(relativePath);
    internal static IReadOnlyList<Guid> TScheduleRemovableResolve(
        IEnumerable<Guid> workIds, IReadOnlyDictionary<Guid, LWorkState> states) =>
        LSchedule.LScheduleRemovableResolve(workIds, states);
    internal static bool TJobCollisionCheck(string output, IReadOnlyList<string> sources) =>
        LJob.LJobCollisionCheck(output, sources);
    internal static IReadOnlyList<string> TEncodeGeometryRead(LWorkCrop crop) => LEncodeVideo.LEncodeGeometryRead(crop);

    internal static IReadOnlyList<LSectionPaletteFile> TSectionPaletteLoad(string folder) =>
        LSectionPalette.LSectionPaletteLoad(folder);
    internal static LSectionPaletteFile? TSectionPaletteRead(string path) => LSectionPalette.LSectionPaletteRead(path);
    internal static bool TSectionPaletteSave(string path, string name, string[] colors) =>
        LSectionPalette.LSectionPaletteSave(path, name, colors);
    internal static bool TSectionPaletteDelete(string path) => LSectionPalette.LSectionPaletteDelete(path);
    internal static LSectionImportResult TSectionPaletteImport(
        string folder, string source, string name, out string target) =>
        LSectionPalette.LSectionPaletteImport(folder, source, name, out target);
    internal static IReadOnlyList<string> TSectionHiddenLoad(string folder) => LSectionPalette.LSectionHiddenLoad(folder);
    internal static bool TSectionHiddenSave(string folder, IReadOnlyList<string> hidden) =>
        LSectionPalette.LSectionHiddenSave(folder, hidden);

    internal static Task<LMediaScanResult> TMediaPathScan(IReadOnlyList<string> paths, CancellationToken token = default) =>
        LMedia.LMediaPathScan(paths, token);

    internal static bool TUsherFileExist(string? path) => LUsher.LUsherFileExist(path);
    internal static bool TUsherFolderExist(string? path) => LUsher.LUsherFolderExist(path);

    internal static void TLocalizationSeamSet(
        Func<IEnumerable<string>>? names, Func<string, string?>? text, Action<string, Exception?>? trace)
    {
        LLocalization.LLocalizationNamesSeam = names;
        LLocalization.LLocalizationTextSeam = text;
        LLocalization.LLocalizationTraceSeam = trace;
    }
    internal static void TLocalizationLoad(string? language) => LLocalization.LLocalizationLoad(language);
    internal static string TLocalizationTextRead(string key) => LLocalization.LLocalizationTextRead(key);
    internal static string TLocalizationLanguageRead() => LLocalization.LLocalizationLanguageRead();
    internal static IReadOnlyDictionary<string, string> TLocalizationLanguagesRead() =>
        LLocalization.LLocalizationLanguagesRead();
}
