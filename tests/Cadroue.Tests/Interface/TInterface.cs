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
    internal static bool TClassifierMatch(LSceneFunnelRule rule, string name) =>
        LClassifier.LClassifierMatch(rule, name);

    internal static LRemedyPlan TRemedyPlanCreate(IReadOnlyList<LDossier> dossiers) =>
        LRemedy.LRemedyPlanCreate(dossiers);

    internal static LDossier? TFlawContainerResolve(string probeError, string copyError) =>
        LFlawMux.LFlawContainerResolve(probeError, copyError);

    internal static LDossier? TFlawTransportResolve(string probeReport, string copyError) =>
        LFlawMux.LFlawTransportResolve(probeReport, copyError);

    internal static LDossier? TFlawTruncationResolve(string probeError, string copyError) =>
        LFlawMux.LFlawTruncationResolve(probeError, copyError);

    internal static LDossier? TFlawMetadataResolve(string probeReport) =>
        LFlawMux.LFlawMetadataResolve(probeReport);

    internal static LDossier? TFlawIndexResolve(string indexedError, string ignidxError, string seekError) =>
        LFlawMux.LFlawIndexResolve(indexedError, ignidxError, seekError);

    internal static LDossier? TFlawFramingResolve(string copyError, string probeReport) =>
        LFlawStream.LFlawFramingResolve(copyError, probeReport);

    internal static LDossier? TFlawConfigResolve(string probeReport, string decodeError) =>
        LFlawStream.LFlawConfigResolve(probeReport, decodeError);

    internal static LDossier? TFlawTimingResolve(string packetReport) =>
        LFlawStream.LFlawTimingResolve(packetReport);

    internal static LDossier? TFlawSecondaryResolve(string streamReport, string chapterReport, string secondaryError) =>
        LFlawSecondary.LFlawSecondaryResolve(streamReport, chapterReport, secondaryError);

    internal static LDossier? TFlawCodedResolve(string decodeError) =>
        LFlawCoded.LFlawCodedResolve(decodeError);

    internal static LDossier? TFlawFfvoneResolve(string probeReport, string crcError) =>
        LFlawFfvone.LFlawFfvoneResolve(probeReport, crcError);

    internal static LDossier TDossierDefectCreate(
        string defect,
        LDossierCategory category,
        LDossierPreservation preservation = LDossierPreservation.LDossierPreservationExact) =>
        new(
            defect, 1.0, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, preservation, string.Empty, string.Empty,
            string.Empty, LDossierValidation.LDossierValidationPassed, category);

    internal static int TClassifierRouteRead(IReadOnlyList<LSceneFunnelRule> rules, string name) =>
        LClassifier.LClassifierRouteRead(rules, name);

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

    internal static (List<LPiece> Sections, int? Active)? TPieceAdd(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, TimeSpan duration, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceAdd(sections, cursor, duration, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active)? TPieceEndCreate(
        IReadOnlyList<LPiece> sections, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndCreate(sections, cursor, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active, bool Added)? TPieceStartSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, TimeSpan duration,
        int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceOriginSet(sections, activeIndex, cursor, duration, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int? Active, bool Added)? TPieceEndSet(
        IReadOnlyList<LPiece> sections, int? activeIndex, TimeSpan cursor, int colorIndex, bool overlapAllowed) =>
        LPiece.LPieceEndSet(sections, activeIndex, cursor, colorIndex, overlapAllowed);

    internal static (List<LPiece> Sections, int First, int Second)? TPieceDivide(
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
        TimeSpan duration, TimeSpan cursor, int direction) =>
        LKeyframeOrchestrator.LKeyframeMoveResolve(keyframes, scannedSpans, duration, cursor, direction);

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

    internal static LPreviewState TPreviewDefaultCreate() => LPreviewState.LPreviewDefaultCreate();
    internal static LColor TColorCreate(double brightness, double contrast, double saturation, double hue) =>
        new(brightness, contrast, saturation, hue);
    internal static LColor TColorGammaCreate(double gamma) =>
        new LColor(0, 1, 1, 0) { LColorGamma = gamma };
    internal static LColor TColorGammaCreate(
        double global, double red, double green, double blue, double protection) =>
        new LColor(0, 1, 1, 0)
        {
            LColorGamma = global,
            LColorGammaRed = red,
            LColorGammaGreen = green,
            LColorGammaBlue = blue,
            LColorHighlightProtection = protection
        };
    internal static LRotateFlip TRotateFlipCreate(LRotateKind rotate, bool horizontal, bool vertical) =>
        new(rotate, horizontal, vertical);
    internal static LPreviewState TPreviewColorChange(LPreviewState state, LColor color) => state.LColorChange(color);
    internal static LCropbox TCropboxCreate(double x, double y, double width, double height) =>
        new(x, y, width, height);
    internal static LWorkCrop TCropboxOrientationResolve(LWorkCrop crop, int rotation, bool horizontal, bool vertical) =>
        LCropbox.LCropboxOrientationResolve(crop, rotation, horizontal, vertical);
    internal static LPreviewState TPreviewCropboxChange(LPreviewState state, LCropbox? cropbox) =>
        state.LCropboxChange(cropbox);
    internal static LPreviewState TPreviewRotateChange(LPreviewState state, LRotateFlip rotateFlip) =>
        state.LRotateFlipChange(rotateFlip);
    internal static LColor TPreviewColorResolve(LWorkVideo video) => LPreview.LPreviewColorResolve(video);
    internal static TimeSpan TPreviewPositionResolve(TimeSpan position, TimeSpan? videoEnd) =>
        LPreview.LPreviewPositionResolve(position, videoEnd);
    internal static string TPreviewFilterResolve(LPreviewState state) => LPreview.LPreviewFilterResolve(state);

    internal static LColorKind? TColorKindParse(string token) => LColor.LColorKindParse(token);
    internal static string TColorKindFormat(LColorKind kind) => LColor.LColorKindFormat(kind);
    internal static LEditPlan TEditPersistentRead(LSidecarEditRecord record) => LEdit.LEditPersistentRead(record);
    internal static LEditPlan TEditPlanResolve(
        LEditPlan? saved, LEditPlan? persistent, bool cropPersistent = false, bool skipPersistent = false) =>
        LEdit.LEditPlanResolve(saved, persistent, cropPersistent, skipPersistent);
    internal static LWorkAudio TAudioPlanResolve(
        LWorkAudio? saved, LWorkAudio? persistent, bool skipPersistent, bool skipApply) =>
        LAudio.LAudioPlanResolve(saved, persistent, skipPersistent, skipApply);
    internal static LWorkAudio TWorkAudioCreate(IReadOnlyList<LWorkAudioStep> steps, bool skip) =>
        new(steps) { LWorkAudioSkip = skip };
    internal static LEditPlan TEditPlanCreate(LWorkCrop crop, LWorkVideo video, bool cropApply) =>
        new(crop, video, cropApply);
    internal static LSidecarEditRecord TEditPersistentCreate(LEditPlan plan) => LEdit.LEditPersistentCreate(plan);
    internal static LWorkVideo TEditVideoCreate(IReadOnlyList<LWorkVideoStep> steps, bool mpvOnlyCapable) =>
        LEdit.LEditVideoCreate(steps, mpvOnlyCapable);
    internal static LWorkVideo TEditVideoCreate(
        IReadOnlyList<LWorkVideoStep> steps, bool mpvOnlyCapable, bool eqCapable) =>
        LEdit.LEditVideoCreate(steps, mpvOnlyCapable, eqCapable);
    internal static LSidecarEditRecord TSidecarEditCreate(string kind, bool active, double value) =>
        new()
        {
            LSidecarSteps = new List<LSidecarVideoStep>
            {
                new() { LSidecarKind = kind, LSidecarActive = active, LSidecarValue = value }
            }
        };
    internal static LSidecarEditRecord TSidecarEditCreate(
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

    internal static LWorkCrop TWorkCropCreate() => LWorkCrop.LWorkCropCreate();
    internal static LWorkCrop TWorkCropCreate(
        int left, int top, int right, int bottom, int rotation, bool horizontal, bool vertical) =>
        new(left, top, right, bottom, rotation, horizontal, vertical);
    internal static LWorkVideo TWorkVideoCreate() => LWorkVideo.LWorkVideoCreate();
    internal static LWorkVideo TWorkVideoCreate(IReadOnlyList<LWorkVideoStep> steps) => new(steps);
    internal static LWorkBand TWorkBandCreate(double frequency, double gain) => new(frequency, gain);
    internal static LWorkAudio TWorkAudioCreate(IReadOnlyList<LWorkAudioStep> steps) => new(steps);
    internal static LWorkMedia TWorkMediaCreate(int width, int height, double rate, long durationMs, bool audio) =>
        new(width, height, rate, durationMs, audio);
    internal static LSplitSectionDescription TSplitSectionCreate(
        TimeSpan start, TimeSpan end, string name, bool hidden = false) =>
        new(start, end, name, LSplitSectionHidden: hidden);
    internal static LSplitWorkDescription TSplitDescriptionCreate(
        string? source, IReadOnlyList<LSplitSectionDescription> sections, LEncoding output) =>
        new(source, sections, output);
    internal static LConvertWorkDescription TConvertDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LEditWorkDescription TEditDescriptionCreate(
        string? source, TimeSpan duration, LWorkCrop crop, LWorkVideo video, LEncoding output) =>
        new(source, duration, crop, video, output);
    internal static LFixWorkDescription TFixDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LWorkGroup TWorkGroupCreate(string name, IReadOnlyList<string> sources) => new(name, sources);
    internal static LWorkVideoStep TWorkBrightnessCreate(bool active, double value) =>
        LWorkVideoStep.LWorkBrightnessCreate(active, value);
    internal static LWorkVideoStep TWorkContrastCreate(bool active, double value) =>
        LWorkVideoStep.LWorkContrastCreate(active, value);
    internal static LWorkVideoStep TWorkSaturationCreate(bool active, double value) =>
        LWorkVideoStep.LWorkSaturationCreate(active, value);
    internal static LWorkVideoStep TWorkExposureCreate(bool active, double value) =>
        LWorkVideoStep.LWorkExposureCreate(active, value);
    internal static LWorkVideoStep TWorkGammaCreate(bool active, double value) =>
        LWorkVideoStep.LWorkGammaCreate(active, value);
    internal static LWorkVideoStep TWorkGammaCreate(
        bool active, double global, double red, double green, double blue, double protection) =>
        LWorkVideoStep.LWorkGammaCreate(active, global, red, green, blue, protection);
    internal static LWorkVideoStep TWorkWhitebalanceCreate(
        bool active, LWhitebalanceMethod method, double saturation) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(active, method, saturation);
    internal static LWorkVideoStep TWorkManualCreate(
        bool active, double saturation,
        double red, double green, double blue,
        int sampleRed, int sampleGreen, int sampleBlue) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(
            active, LWhitebalanceMethod.LWhitebalanceMethodManual, saturation,
            red, green, blue, sampleRed, sampleGreen, sampleBlue);
    internal static LWorkWhitebalanceSettings TWorkWhitebalanceRead(LWorkVideoStep step) =>
        step.LWorkWhitebalanceRead();
    internal static LWorkVideoStep TWorkCurveCreate(
        bool active,
        IReadOnlyList<LWorkCurvePoint>? master = null,
        IReadOnlyList<LWorkCurvePoint>? red = null,
        IReadOnlyList<LWorkCurvePoint>? green = null,
        IReadOnlyList<LWorkCurvePoint>? blue = null) =>
        LWorkVideoStep.LWorkCurveCreate(active, master, red, green, blue);
    internal static LWorkCurvePoint TWorkPointCreate(double input, double output) =>
        new(input, output);
    internal static LWorkCurveSettings TWorkCurveRead(LWorkVideoStep step) => step.LWorkCurveRead();
    internal static string TWorkCurveFormat(LWorkVideoStep step) => step.LWorkCurveFormat();
    internal static LWorkVideoStep TWorkMalformedCreate(
        LWhitebalanceMethod method, double value, double saturation) =>
        new(LColorKind.LColorKindWhitebalance, true, value)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, saturation)
        };
    internal static LWorkVideoStep TWorkBrokenCreate(
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
    internal static LWorkVideoStep TWorkStrayCreate(
        LWhitebalanceMethod method, double red, int sampleRed) =>
        new(LColorKind.LColorKindWhitebalance, true, 100)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, 100)
            {
                LWorkWhitebalanceRed = red,
                LWorkSampleRed = sampleRed
            }
        };
    internal static string TWorkDiagnosticRead(LWorkVideoStep step) => step.LWorkDiagnosticRead();
    internal static LWorkItem? TWorkRecordMatch(LWorkItem work)
    {
        string json = LWorkRecord.LWorkRecordCreate(work).LWorkJsonCreate();
        return LWorkRecord.LWorkRecordParse(json)?.LWorkItemCreate();
    }
    internal static LSidecarEditRecord TSidecarEditMatch(LSidecarEditRecord record) =>
        System.Text.Json.JsonSerializer.Deserialize<LSidecarEditRecord>(
            System.Text.Json.JsonSerializer.Serialize(record))!;
    internal static LWorkAudioStep TWorkVolumeCreate(bool active, double gain) =>
        LWorkAudioStep.LWorkVolumeCreate(active, gain);
    internal static LWorkAudioStep TWorkNormalizeCreate(
        bool active, LLeveling mode, double target, double peak, double range, bool twoPass,
        double frame, double gauss, double maxGain, double compress) =>
        LWorkAudioStep.LWorkNormalizeCreate(active, mode, target, peak, range, twoPass, frame, gauss, maxGain, compress);
    internal static LWorkAudioStep TWorkNoiseCreate(
        bool active, double reduction, double floor, bool outputNoise, LGrain grain,
        double smooth, double adaptivity, double residual) =>
        LWorkAudioStep.LWorkNoiseCreate(active, reduction, floor, outputNoise, grain, smooth, adaptivity, residual);
    internal static LWorkAudioStep TWorkHighCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkHighCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TWorkLowCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkLowCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TWorkEqualizerCreate(bool active, IReadOnlyList<LWorkBand> bands) =>
        LWorkAudioStep.LWorkEqualizerCreate(active, bands);
    internal static string TWorkAudioFormat(LWorkAudio audio) => audio.LWorkAudioFormat();

    internal static IReadOnlyList<string> TContourTokensRead() => LContourCatalog.LContourTokensRead();
    internal static double[]? TContourGainsRead(string token) => LContourCatalog.LContourGainsRead(token);
    internal static bool TContourMatch(
        IReadOnlyList<double> frequencies, IReadOnlyList<double> gains, double[] expected) =>
        LContourCatalog.LContourMatch(frequencies, gains, expected);
    internal static string? TContourPresetFind(IReadOnlyList<double> frequencies, IReadOnlyList<double> gains) =>
        LContourCatalog.LContourPresetFind(frequencies, gains);

    internal static string TGrainFormat(LGrain grain) => LGrainCatalog.LGrainFormat(grain);
    internal static LGrain TGrainParse(string token) => LGrainCatalog.LGrainParse(token);
    internal static LGrainPreset? TGrainRead(string token) => LGrainCatalog.LGrainRead(token);
    internal static string? TGrainMatch(
        double reduction, double floor, double smooth, double adaptivity, double residual, LGrain grain) =>
        LGrainCatalog.LGrainMatch(reduction, floor, smooth, adaptivity, residual, grain);

    internal static LBridgePlan TBridgeResolve(
        IReadOnlyList<TimeSpan> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static LBridgePlan TBridgeResolve(
        IReadOnlyList<LKeyframeEntry> keyframes, TimeSpan origin, TimeSpan end, bool openEnd = false) =>
        LBridge.LBridgeRegionResolve(keyframes, origin, end, openEnd);
    internal static bool TBridgeEndCheck(TimeSpan end, TimeSpan duration, double framerate) =>
        LBridge.LBridgeEndCheck(end, duration, framerate);
    internal static bool TBridgeLeadingNormalize(byte[] bytes) => LBridge.LBridgeLeadingNormalize(bytes);

    internal static LPassbandPreset? TPassbandRead(bool high, string token) => LPassband.LPassbandRead(high, token);
    internal static string? TPassbandMatch(bool high, double frequency, int stages, int poles, double resonance) =>
        LPassband.LPassbandMatch(high, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TPassbandStepCreate(bool high, bool active) =>
        LPassband.LPassbandStepCreate(high, active);

    internal static (double Target, double Peak, double Range)? TLevelingLoudnessRead(string token) =>
        LLevelingCatalog.LLevelingLoudnessRead(token);
    internal static (double Frame, double Gauss, double MaxGain, double Compress)? TLevelingDynamicRead(string token) =>
        LLevelingCatalog.LLevelingDynamicRead(token);
    internal static string? TLevelingLoudnessMatch(double target, double peak, double range) =>
        LLevelingCatalog.LLevelingLoudnessMatch(target, peak, range);
    internal static string? TLevelingDynamicMatch(double frame, double gauss, double maxGain, double compress) =>
        LLevelingCatalog.LLevelingDynamicMatch(frame, gauss, maxGain, compress);
    internal static (double Target, double Peak, double Range, bool TwoPass, double Frame, double Gauss, double MaxGain, double Compress)
        TLevelingDefaultRead() => LLevelingCatalog.LLevelingDefaultRead();

    internal static bool TRetentionExpiredCheck(DateTime writeUtc, DateTime nowUtc, int days) =>
        LRetention.LRetentionExpiredCheck(writeUtc, nowUtc, days);
    internal static bool TRetentionExcludedCheck(string relativePath) => LRetention.LRetentionExcludedCheck(relativePath);
    internal static IReadOnlyList<Guid> TScheduleRemovableResolve(
        IEnumerable<Guid> workIds, IReadOnlyDictionary<Guid, LWorkState> states) =>
        LSchedule.LScheduleRemovableResolve(workIds, states);
    internal static bool TJobCollisionCheck(string output, IReadOnlyList<string> sources) =>
        LJob.LJobCollisionCheck(output, sources);
    internal static IReadOnlyList<string> TEncodeGeometryRead(LWorkCrop crop) => LEncodeVideo.LEncodeGeometryRead(crop);

    internal static LWorkItem? TAudioItemCreate(
        LWorkPriority priority, string? source, LWorkAudio processing, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, Func<string, TimeSpan> durationRead, Guid batchId = default) =>
        LAudio.LAudioItemCreate(priority, source, processing, output, tab, infoLog, errorLog, durationRead, batchId);

    internal static IReadOnlyList<LWorkItem> TConvertItemsCreate(
        LWorkPriority priority, LConvertWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LConvert.LConvertItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> TEditItemsCreate(
        LWorkPriority priority, LEditWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LEdit.LEditItemsCreate(priority, description, tab, infoLog, errorLog, batchId);

    internal static IReadOnlyList<LWorkItem> TFixItemsCreate(
        LWorkPriority priority, LFixWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LFix.LFixItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> TMergeItemsCreate(
        LWorkPriority priority, IReadOnlyList<LWorkGroup> groups, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, IReadOnlyDictionary<string, Guid>? relays = null) =>
        LMerge.LMergeItemsCreate(priority, groups, output, tab, infoLog, errorLog, relays);

    internal static IReadOnlyList<LWorkItem> TSplitItemsCreate(
        LWorkPriority priority, LSplitWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LSplit.LSplitItemsCreate(priority, description, tab, infoLog, errorLog, batchId);
}
