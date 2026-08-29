using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

/// <summary>
/// Test-side boundary for production operations. Every member is a transparent relay:
/// it delegates to exactly one production operation and does not alter its inputs or result.
/// </summary>
internal static class TInterface
{
    internal static bool ClassifierMatch(LSceneFunnelRule rule, string name) =>
        LClassifier.LClassifierMatch(rule, name);

    internal static LRemedyPlan RemedyPlanCreate(IReadOnlyList<LDossier> dossiers) =>
        LRemedy.LRemedyPlanCreate(dossiers);

    internal static LDossier? FlawContainerResolve(string probeError, string copyError) =>
        LFlawMux.LFlawContainerResolve(probeError, copyError);

    internal static LDossier? FlawTransportResolve(string probeReport, string copyError) =>
        LFlawMux.LFlawTransportResolve(probeReport, copyError);

    internal static LDossier? FlawTruncationResolve(string probeError, string copyError) =>
        LFlawMux.LFlawTruncationResolve(probeError, copyError);

    internal static LDossier? FlawMetadataResolve(string probeReport) =>
        LFlawMux.LFlawMetadataResolve(probeReport);

    internal static LDossier? FlawIndexResolve(string indexedError, string ignidxError, string seekError) =>
        LFlawMux.LFlawIndexResolve(indexedError, ignidxError, seekError);

    internal static LDossier? FlawFramingResolve(string copyError, string probeReport) =>
        LFlawStream.LFlawFramingResolve(copyError, probeReport);

    internal static LDossier? FlawConfigResolve(string probeReport, string decodeError) =>
        LFlawStream.LFlawConfigResolve(probeReport, decodeError);

    internal static LDossier? FlawTimingResolve(string packetReport) =>
        LFlawStream.LFlawTimingResolve(packetReport);

    internal static LDossier? FlawSecondaryResolve(string streamReport, string chapterReport, string secondaryError) =>
        LFlawSecondary.LFlawSecondaryResolve(streamReport, chapterReport, secondaryError);

    internal static LDossier DossierDefectCreate(
        string defect,
        LDossierCategory category,
        LDossierPreservation preservation = LDossierPreservation.LDossierPreservationExact) =>
        new(
            defect, 1.0, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, preservation, string.Empty, string.Empty,
            string.Empty, LDossierValidation.LDossierValidationPassed, category);

    internal static int ClassifierRouteRead(IReadOnlyList<LSceneFunnelRule> rules, string name) =>
        LClassifier.LClassifierRouteRead(rules, name);

    internal static IReadOnlyList<LSeriesGroup> SeriesResolve(
        IReadOnlyList<string> paths,
        bool strict,
        LSeriesNameMode nameMode = LSeriesNameMode.LSeriesNameBase) =>
        LSeries.LSeriesResolve(paths, strict, nameMode);

    internal static IReadOnlyList<LPiece> PieceValidSelect(IReadOnlyList<LPiece> sections, TimeSpan duration) =>
        LPiece.LPieceValidSelect(sections, duration);

    internal static bool PieceInsideCheck(
        IReadOnlyList<LPiece> sections, TimeSpan time, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceInsideCheck(sections, time, skipIndex, overlapAllowed);

    internal static TimeSpan PieceLimitRead(
        IReadOnlyList<LPiece> sections, TimeSpan from, TimeSpan ceiling, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceLimitRead(sections, from, ceiling, skipIndex, overlapAllowed);

    internal static TimeSpan PieceFloorRead(
        IReadOnlyList<LPiece> sections, TimeSpan until, int skipIndex, bool overlapAllowed) =>
        LPiece.LPieceFloorRead(sections, until, skipIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active)? PieceAdd(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, TimeSpan duration, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceAdd(sections, cursor, duration, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active)? PieceEndCreate(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndCreate(sections, cursor, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active, bool Added)? PieceStartSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, TimeSpan duration,
        int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceOriginSet(sections, activeIndex, cursor, duration, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active, bool Added)? PieceEndSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndSet(sections, activeIndex, cursor, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int First, int Second)? PieceDivide(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, int colorIndex) =>
        LPiece.LPieceDivide(sections, activeIndex, cursor, colorIndex);

    internal static LSpool SpoolCreate(TimeSpan duration) => new(duration);
    internal static TimeSpan SpoolStepResolve(LSpool spool, int count) => spool.LSpoolStepResolve(count);
    internal static void SpoolZoom(LSpool spool, TimeSpan cursor, int steps) => spool.LSpoolZoom(cursor, steps);
    internal static void SpoolStartSet(LSpool spool, TimeSpan start) => spool.LSpoolStartSet(start);
    internal static void SpoolEndSet(LSpool spool, TimeSpan end) => spool.LSpoolEndSet(end);

    internal static IReadOnlyList<LKeyframeEntry> KeyframeVisibleResolve(
        IReadOnlyList<LKeyframeEntry> keyframes, TimeSpan cursor, LSpool spool) =>
        LKeyframeView.LKeyframeVisibleResolve(keyframes, cursor, spool);

    internal static IReadOnlyList<LKeyframeScanRange> KeyframeCoverageResolve(
        IReadOnlyList<LKeyframeScanRange> ranges, LSpool spool, bool wholeMedia) =>
        LKeyframeView.LKeyframeCoverageResolve(ranges, spool, wholeMedia);

    internal static LKeyframeMoveResult KeyframeMoveResolve(
        IReadOnlyCollection<long> keyframes, IReadOnlySet<int> scannedSpans,
        TimeSpan duration, TimeSpan cursor, int direction) =>
        LKeyframeOrchestrator.LKeyframeMoveResolve(keyframes, scannedSpans, duration, cursor, direction);

    internal static LPreferenceState PreferenceDefaultCreate() => LPreferenceState.LPreferenceDefaultCreate();
    internal static LPreferenceState PreferenceCreate(int cleanupDays = 30) =>
        new() { LPreferenceCleanupDays = cleanupDays };
    internal static LPreferenceState PreferenceClone(LPreferenceState state) => state.LPreferenceClone();
    internal static void PreferenceNormalize(LPreferenceState state) => state.LPreferenceNormalize();
    internal static IEnumerable<string> PreferenceDifferenceRead(LPreferenceState state, LPreferenceState before) =>
        state.LPreferenceDifferenceRead(before);
    internal static bool PreferencePresetGroupFoldedRead(
        LPreferenceState state,
        string groupName,
        bool fallback = true) =>
        state.LPreferenceFoldRead(groupName, fallback);
    internal static void PreferencePresetGroupFoldedSet(
        LPreferenceState state,
        string groupName,
        bool folded) =>
        state.LPreferenceFold[groupName] = folded;

    internal static LGroupSelection GroupSelectionCreate(
        bool groupAuto = false,
        bool groupStrict = true,
        LSeriesNameMode nameMode = LSeriesNameMode.LSeriesNameBase) =>
        new(groupAuto, groupStrict, nameMode);

    internal static void GroupNameModeRequest(LGroupSelection selection, LSeriesNameMode mode) =>
        selection.LGroupModeChange(mode);

    internal static IReadOnlyList<LSeriesGroup> GroupResolve(LGroupSelection selection, IReadOnlyList<string> paths) =>
        selection.LGroupResolve(paths);

    internal static LPreviewState PreviewDefaultCreate() => LPreviewState.LPreviewDefaultCreate();
    internal static LColor ColorCreate(double brightness, double contrast, double saturation, double hue) =>
        new(brightness, contrast, saturation, hue);
    internal static LColor ColorGammaCreate(double gamma) =>
        new LColor(0, 1, 1, 0) { LColorGamma = gamma };
    internal static LColor ColorGammaCreate(
        double global, double red, double green, double blue, double protection) =>
        new LColor(0, 1, 1, 0)
        {
            LColorGamma = global,
            LColorGammaRed = red,
            LColorGammaGreen = green,
            LColorGammaBlue = blue,
            LColorHighlightProtection = protection
        };
    internal static LRotateFlip RotateFlipCreate(LRotateKind rotate, bool horizontal, bool vertical) =>
        new(rotate, horizontal, vertical);
    internal static LPreviewState PreviewColorChange(LPreviewState state, LColor color) => state.LColorChange(color);
    internal static LCropbox CropboxCreate(double x, double y, double width, double height) =>
        new(x, y, width, height);
    internal static LWorkCrop CropboxOrientationResolve(LWorkCrop crop, int rotation, bool horizontal, bool vertical) =>
        LCropbox.LCropboxOrientationResolve(crop, rotation, horizontal, vertical);
    internal static LPreviewState PreviewCropboxChange(LPreviewState state, LCropbox? cropbox) =>
        state.LCropboxChange(cropbox);
    internal static LPreviewState PreviewRotateFlipChange(LPreviewState state, LRotateFlip rotateFlip) =>
        state.LRotateFlipChange(rotateFlip);
    internal static LColor PreviewColorResolve(LWorkVideo video) => LPreview.LPreviewColorResolve(video);
    internal static TimeSpan PreviewPositionResolve(TimeSpan position, TimeSpan? videoEnd) =>
        LPreview.LPreviewPositionResolve(position, videoEnd);
    internal static string PreviewMpvFilterResolve(LPreviewState state) => LPreview.LPreviewFilterResolve(state);

    internal static LColorKind? ColorKindParse(string token) => LColor.LColorKindParse(token);
    internal static string ColorKindFormat(LColorKind kind) => LColor.LColorKindFormat(kind);
    internal static LEditPlan EditPersistentRead(LSidecarEditRecord record) => LEdit.LEditPersistentRead(record);
    internal static LEditPlan EditPlanResolve(
        LEditPlan? saved, LEditPlan? persistent, bool cropPersistent = false, bool skipPersistent = false) =>
        LEdit.LEditPlanResolve(saved, persistent, cropPersistent, skipPersistent);
    internal static LWorkAudio AudioPlanResolve(
        LWorkAudio? saved, LWorkAudio? persistent, bool skipPersistent, bool skipApply) =>
        LAudio.LAudioPlanResolve(saved, persistent, skipPersistent, skipApply);
    internal static LWorkAudio WorkAudioCreate(IReadOnlyList<LWorkAudioStep> steps, bool skip) =>
        new(steps) { LWorkAudioSkip = skip };
    internal static LEditPlan EditPlanCreate(LWorkCrop crop, LWorkVideo video, bool cropApply) =>
        new(crop, video, cropApply);
    internal static LSidecarEditRecord EditPersistentCreate(LEditPlan plan) => LEdit.LEditPersistentCreate(plan);
    internal static LWorkVideo EditVideoCreate(IReadOnlyList<LWorkVideoStep> steps, bool mpvOnlyCapable) =>
        LEdit.LEditVideoCreate(steps, mpvOnlyCapable);
    internal static LWorkVideo EditVideoCreate(
        IReadOnlyList<LWorkVideoStep> steps, bool mpvOnlyCapable, bool eqCapable) =>
        LEdit.LEditVideoCreate(steps, mpvOnlyCapable, eqCapable);
    internal static LSidecarEditRecord SidecarEditRecordCreate(string kind, bool active, double value) =>
        new()
        {
            LSidecarSteps = new List<LSidecarVideoStep>
            {
                new() { LSidecarKind = kind, LSidecarActive = active, LSidecarValue = value }
            }
        };
    internal static LSidecarEditRecord SidecarEditRecordCreate(
        string kind, bool active, double value, double? red, double? green, double? blue, double? protection) =>
        new()
        {
            LSidecarSteps = new List<LSidecarVideoStep>
            {
                new()
                {
                    LSidecarKind = kind,
                    LSidecarActive = active,
                    LSidecarValue = value,
                    LSidecarGammaRed = red,
                    LSidecarGammaGreen = green,
                    LSidecarGammaBlue = blue,
                    LSidecarGammaHighlight = protection
                }
            }
        };

    internal static LWorkCrop WorkCropCreate() => LWorkCrop.LWorkCropCreate();
    internal static LWorkCrop WorkCropCreate(
        int left, int top, int right, int bottom, int rotation, bool horizontal, bool vertical) =>
        new(left, top, right, bottom, rotation, horizontal, vertical);
    internal static LWorkVideo WorkVideoCreate() => LWorkVideo.LWorkVideoCreate();
    internal static LWorkVideo WorkVideoCreate(IReadOnlyList<LWorkVideoStep> steps) => new(steps);
    internal static LWorkBand WorkBandCreate(double frequency, double gain) => new(frequency, gain);
    internal static LWorkAudio WorkAudioCreate(IReadOnlyList<LWorkAudioStep> steps) => new(steps);
    internal static LWorkMedia WorkMediaCreate(int width, int height, double rate, long durationMs, bool audio) =>
        new(width, height, rate, durationMs, audio);
    internal static LSplitSectionDescription SplitSectionCreate(
        TimeSpan start, TimeSpan end, string name, bool hidden = false) =>
        new(start, end, name, LSplitSectionHidden: hidden);
    internal static LSplitWorkDescription SplitDescriptionCreate(
        string? source, IReadOnlyList<LSplitSectionDescription> sections, LEncoding output) =>
        new(source, sections, output);
    internal static LConvertWorkDescription ConvertDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LEditWorkDescription EditDescriptionCreate(
        string? source, TimeSpan duration, LWorkCrop crop, LWorkVideo video, LEncoding output) =>
        new(source, duration, crop, video, output);
    internal static LFixWorkDescription FixDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LWorkGroup WorkGroupCreate(string name, IReadOnlyList<string> sources) => new(name, sources);
    internal static LWorkVideoStep WorkBrightnessCreate(bool active, double value) =>
        LWorkVideoStep.LWorkBrightnessCreate(active, value);
    internal static LWorkVideoStep WorkContrastCreate(bool active, double value) =>
        LWorkVideoStep.LWorkContrastCreate(active, value);
    internal static LWorkVideoStep WorkSaturationCreate(bool active, double value) =>
        LWorkVideoStep.LWorkSaturationCreate(active, value);
    internal static LWorkVideoStep WorkExposureCreate(bool active, double value) =>
        LWorkVideoStep.LWorkExposureCreate(active, value);
    internal static LWorkVideoStep WorkGammaCreate(bool active, double value) =>
        LWorkVideoStep.LWorkGammaCreate(active, value);
    internal static LWorkVideoStep WorkGammaCreate(
        bool active, double global, double red, double green, double blue, double protection) =>
        LWorkVideoStep.LWorkGammaCreate(active, global, red, green, blue, protection);
    internal static LWorkVideoStep WorkWhitebalanceCreate(
        bool active, LWhitebalanceMethod method, double saturation) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(active, method, saturation);
    internal static LWorkVideoStep WorkWhitebalanceManualCreate(
        bool active, double saturation,
        double red, double green, double blue,
        int sampleRed, int sampleGreen, int sampleBlue) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(
            active, LWhitebalanceMethod.LWhitebalanceMethodManual, saturation,
            red, green, blue, sampleRed, sampleGreen, sampleBlue);
    internal static LWorkWhitebalanceSettings WorkWhitebalanceRead(LWorkVideoStep step) =>
        step.LWorkWhitebalanceRead();
    internal static LWorkVideoStep WorkCurveCreate(
        bool active,
        IReadOnlyList<LWorkCurvePoint>? master = null,
        IReadOnlyList<LWorkCurvePoint>? red = null,
        IReadOnlyList<LWorkCurvePoint>? green = null,
        IReadOnlyList<LWorkCurvePoint>? blue = null) =>
        LWorkVideoStep.LWorkCurveCreate(active, master, red, green, blue);
    internal static LWorkCurvePoint WorkCurvePointCreate(double input, double output) =>
        new(input, output);
    internal static LWorkCurveSettings WorkCurveRead(LWorkVideoStep step) => step.LWorkCurveRead();
    internal static string WorkCurveFormat(LWorkVideoStep step) => step.LWorkCurveFormat();
    internal static LWorkVideoStep WorkWhitebalanceMalformedCreate(
        LWhitebalanceMethod method, double value, double saturation) =>
        new(LColorKind.LColorKindWhitebalance, true, value)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, saturation)
        };
    internal static LWorkVideoStep WorkWhitebalanceManualMalformedCreate(
        double saturation, double red, double green, double blue,
        int sampleRed, int sampleGreen, int sampleBlue) =>
        new(LColorKind.LColorKindWhitebalance, true, saturation)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(
                LWhitebalanceMethod.LWhitebalanceMethodManual, saturation)
            {
                LWorkWhitebalanceRed = red,
                LWorkWhitebalanceGreen = green,
                LWorkWhitebalanceBlue = blue,
                LWorkSampleRed = sampleRed,
                LWorkSampleGreen = sampleGreen,
                LWorkSampleBlue = sampleBlue
            }
        };
    internal static LWorkVideoStep WorkWhitebalanceStrayCreate(
        LWhitebalanceMethod method, double red, int sampleRed) =>
        new(LColorKind.LColorKindWhitebalance, true, 100)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, 100)
            {
                LWorkWhitebalanceRed = red,
                LWorkSampleRed = sampleRed
            }
        };
    internal static string WorkVideoStepDiagnosticRead(LWorkVideoStep step) => step.LWorkDiagnosticRead();
    internal static LWorkItem? WorkRecordRoundTrip(LWorkItem work)
    {
        string json = LWorkRecord.LWorkRecordCreate(work).LWorkJsonCreate();
        return LWorkRecord.LWorkRecordParse(json)?.LWorkItemCreate();
    }
    internal static LSidecarEditRecord SidecarEditRecordRoundTrip(LSidecarEditRecord record) =>
        System.Text.Json.JsonSerializer.Deserialize<LSidecarEditRecord>(
            System.Text.Json.JsonSerializer.Serialize(record))!;
    internal static LWorkAudioStep WorkVolumeCreate(bool active, double gain) =>
        LWorkAudioStep.LWorkVolumeCreate(active, gain);
    internal static LWorkAudioStep WorkNormalizeCreate(
        bool active, LLeveling mode, double target, double peak, double range, bool twoPass,
        double frame, double gauss, double maxGain, double compress) =>
        LWorkAudioStep.LWorkNormalizeCreate(active, mode, target, peak, range, twoPass, frame, gauss, maxGain, compress);
    internal static LWorkAudioStep WorkNoiseCreate(
        bool active, double reduction, double floor, bool outputNoise, LGrain grain,
        double smooth, double adaptivity, double residual) =>
        LWorkAudioStep.LWorkNoiseCreate(active, reduction, floor, outputNoise, grain, smooth, adaptivity, residual);
    internal static LWorkAudioStep WorkHighCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkHighCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep WorkLowCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkLowCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep WorkEqualizerCreate(bool active, IReadOnlyList<LWorkBand> bands) =>
        LWorkAudioStep.LWorkEqualizerCreate(active, bands);
    internal static string WorkAudioFormat(LWorkAudio audio) => audio.LWorkAudioFormat();

    internal static IReadOnlyList<string> ContourTokensRead() => LContourCatalog.LContourTokensRead();
    internal static double[]? ContourGainsRead(string token) => LContourCatalog.LContourGainsRead(token);
    internal static bool ContourMatch(
        IReadOnlyList<double> frequencies, IReadOnlyList<double> gains, double[] expected) =>
        LContourCatalog.LContourMatch(frequencies, gains, expected);
    internal static string? ContourPresetFind(IReadOnlyList<double> frequencies, IReadOnlyList<double> gains) =>
        LContourCatalog.LContourPresetFind(frequencies, gains);

    internal static string GrainFormat(LGrain grain) => LGrainCatalog.LGrainFormat(grain);
    internal static LGrain GrainParse(string token) => LGrainCatalog.LGrainParse(token);
    internal static LGrainPreset? GrainRead(string token) => LGrainCatalog.LGrainRead(token);
    internal static string? GrainMatch(
        double reduction, double floor, double smooth, double adaptivity, double residual, LGrain grain) =>
        LGrainCatalog.LGrainMatch(reduction, floor, smooth, adaptivity, residual, grain);

    internal static LBridgePlan BridgeResolve(
        IReadOnlyList<TimeSpan> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static LBridgePlan BridgeResolve(
        IReadOnlyList<LKeyframeEntry> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static bool BridgeEndCheck(TimeSpan end, TimeSpan duration, double framerate) =>
        LBridge.LBridgeEndCheck(end, duration, framerate);
    internal static bool BridgeLeadingNormalize(byte[] bytes) => LBridge.LBridgeLeadingNormalize(bytes);

    internal static LPassbandPreset? PassbandRead(bool high, string token) => LPassband.LPassbandRead(high, token);
    internal static string? PassbandMatch(bool high, double frequency, int stages, int poles, double resonance) =>
        LPassband.LPassbandMatch(high, frequency, stages, poles, resonance);
    internal static LWorkAudioStep PassbandStepCreate(bool high, bool active) =>
        LPassband.LPassbandStepCreate(high, active);

    internal static (double Target, double Peak, double Range)? LevelingLoudnessRead(string token) =>
        LLevelingCatalog.LLevelingLoudnessRead(token);
    internal static (double Frame, double Gauss, double MaxGain, double Compress)? LevelingDynamicRead(string token) =>
        LLevelingCatalog.LLevelingDynamicRead(token);
    internal static string? LevelingLoudnessMatch(double target, double peak, double range) =>
        LLevelingCatalog.LLevelingLoudnessMatch(target, peak, range);
    internal static string? LevelingDynamicMatch(double frame, double gauss, double maxGain, double compress) =>
        LLevelingCatalog.LLevelingDynamicMatch(frame, gauss, maxGain, compress);
    internal static (double Target, double Peak, double Range, bool TwoPass, double Frame, double Gauss, double MaxGain, double Compress)
        LevelingDefaultRead() => LLevelingCatalog.LLevelingDefaultRead();

    internal static bool RetentionExpiredCheck(DateTime writeUtc, DateTime nowUtc, int days) =>
        LRetention.LRetentionExpiredCheck(writeUtc, nowUtc, days);
    internal static bool RetentionExcludedCheck(string relativePath) => LRetention.LRetentionExcludedCheck(relativePath);
    internal static IReadOnlyList<Guid> ScheduleRemovableResolve(
        IEnumerable<Guid> workIds, IReadOnlyDictionary<Guid, LWorkState> states) =>
        LSchedule.LScheduleRemovableResolve(workIds, states);
    internal static bool JobCollisionCheck(string output, IReadOnlyList<string> sources) =>
        LJob.LJobCollisionCheck(output, sources);
    internal static IReadOnlyList<string> EncodeGeometryRead(LWorkCrop crop) => LEncodeVideo.LEncodeGeometryRead(crop);

    internal static LWorkItem? AudioItemCreate(
        LWorkPriority priority, string? source, LWorkAudio processing, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, Func<string, TimeSpan> durationRead, Guid batchId = default) =>
        LAudio.LAudioItemCreate(priority, source, processing, output, tab, infoLog, errorLog, durationRead, batchId);

    internal static IReadOnlyList<LWorkItem> ConvertItemsCreate(
        LWorkPriority priority, LConvertWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LConvert.LConvertItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> EditItemsCreate(
        LWorkPriority priority, LEditWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LEdit.LEditItemsCreate(priority, description, tab, infoLog, errorLog, batchId);

    internal static IReadOnlyList<LWorkItem> FixItemsCreate(
        LWorkPriority priority, LFixWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LFix.LFixItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> MergeItemsCreate(
        LWorkPriority priority, IReadOnlyList<LWorkGroup> groups, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, IReadOnlyDictionary<string, Guid>? relays = null) =>
        LMerge.LMergeItemsCreate(priority, groups, output, tab, infoLog, errorLog, relays);

    internal static IReadOnlyList<LWorkItem> SplitItemsCreate(
        LWorkPriority priority, LSplitWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LSplit.LSplitItemsCreate(priority, description, tab, infoLog, errorLog, batchId);
}
