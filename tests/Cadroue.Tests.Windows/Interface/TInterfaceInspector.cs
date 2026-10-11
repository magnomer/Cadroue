using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceInspector
{
    internal static LInspector TInspectorCreate() => new();
    internal static void TInspectorAttach(LInspector inspector, Action handler) =>
        inspector.LInspectorChanged += handler;
    internal static void TInspectorSourceSet(LInspector inspector, double width, double height) =>
        inspector.LInspectorSourceSet(width, height);
    internal static void TInspectorStepSet(LInspector inspector, string? step) => inspector.LInspectorStepSet(step);
    internal static void TInspectorMinimizedSet(LInspector inspector, bool minimized) =>
        inspector.LInspectorMinimizedSet(minimized);
    internal static bool TInspectorOwnerSet(LInspector inspector, string path) => inspector.LInspectorOwnerSet(path);
    internal static bool TInspectorFailureSet(LInspector inspector, string? path) =>
        inspector.LInspectorFailureSet(path);
    internal static LWorkCrop TInspectorCropRead(LInspector inspector) => inspector.LInspectorCrop.LInspectorCropRead();
    internal static LCropbox? TInspectorRectRead(LInspector inspector) => inspector.LInspectorCrop.LInspectorRectRead();
    internal static LRotateFlip TInspectorRotateRead(LInspector inspector) =>
        inspector.LInspectorCrop.LInspectorRotateRead();
    internal static void TInspectorCropApply(LInspector inspector, LWorkCrop crop, bool apply) =>
        inspector.LInspectorCrop.LInspectorCropApply(crop, apply);
    internal static void TInspectorRatioApply(
        LInspector inspector, bool fixedRatio, bool lenient, int width, int height) =>
        inspector.LInspectorCrop.LInspectorRatioApply(fixedRatio, lenient, width, height);
    internal static void TInspectorCropReset(LInspector inspector) => inspector.LInspectorCrop.LInspectorCropReset();
    internal static void TInspectorPlanApply(LInspector inspector, LEditPlan plan) =>
        inspector.LInspectorPlanApply(plan);
    internal static void TInspectorVideoAttach(LInspector inspector, Action handler) =>
        inspector.LInspectorVideoChanged += handler;
    internal static void TInspectorPersistentAttach(LInspector inspector, Action handler) =>
        inspector.LInspectorPersistentChanged += handler;
    internal static string TInspectorTitleRead(LInspector inspector) => inspector.LInspectorTitleRead();
    internal static bool TInspectorSectionCheck(LInspector inspector, string key) =>
        inspector.LInspectorSectionCheck(key);
    internal static double TInspectorValueCommit(string text, double current, double? least, double? most) =>
        LInspector.LInspectorValueCommit(text, current, least, most);
    internal static string TInspectorValueFormat(string text, double number, string format) =>
        LInspector.LInspectorValueFormat(text, number, format);
    internal static void TInspectorSlideCommit(
        double slider, double current, double least, double most, Action<double> set) =>
        LInspector.LInspectorSlideCommit(slider, current, least, most, set);
    internal static LInspectorTip TInspectorTipResolve(
        LInspector inspector, bool active, bool capable, bool preview, string disabled, string previewKey) =>
        inspector.LInspectorTipResolve(
            active, capable, preview, disabled, previewKey, "Inspector.Common.Apply", "Inspector.Common.Persistent");
    internal static void TInspectorPresetSelect(LInspector inspector, int index) =>
        inspector.LInspectorCrop.LInspectorPresetSelect(index);
    internal static void TInspectorFixedSet(LInspector inspector, bool fixedRatio) =>
        inspector.LInspectorCrop.LInspectorFixedSet(fixedRatio);
    internal static void TInspectorLenientSet(LInspector inspector, bool lenient) =>
        inspector.LInspectorCrop.LInspectorLenientSet(lenient);
    internal static void TInspectorRatioCommit(
        LInspector inspector, string width, string height, bool typed = true) =>
        inspector.LInspectorCrop.LInspectorRatioCommit(width, height, typed);
    internal static string TInspectorRatioFormat(LInspector inspector, bool width, string text) =>
        inspector.LInspectorCrop.LInspectorRatioFormat(width, text);
    internal static string TInspectorNoticeRead(LInspector inspector) =>
        inspector.LInspectorCrop.LInspectorNoticeRead();
    internal static string TInspectorResolutionRead(LInspector inspector) =>
        inspector.LInspectorCrop.LInspectorResolutionRead();
    internal static void TInspectorEdgeCommit(LInspector inspector, int side, string text, bool typed = true) =>
        inspector.LInspectorCrop.LInspectorEdgeCommit(side, text, typed);
    internal static string TInspectorEdgeFormat(LInspector inspector, int side, string text) =>
        inspector.LInspectorCrop.LInspectorEdgeFormat(side, text);
    internal static void TInspectorEdgeReset(LInspector inspector) => inspector.LInspectorCrop.LInspectorEdgeReset();
    internal static void TInspectorCropSet(
        LInspector inspector, LCropbox? drawn, int drive, int anchorX, int anchorY) =>
        inspector.LInspectorCrop.LInspectorCropSet(drawn, drive, anchorX, anchorY);
    internal static void TInspectorRotateSelect(LInspector inspector, int index) =>
        inspector.LInspectorCrop.LInspectorRotateSelect(index);
    internal static void TInspectorFlipSet(LInspector inspector, bool horizontal, bool flipped) =>
        inspector.LInspectorCrop.LInspectorFlipSet(horizontal, flipped);
    internal static void TInspectorApplySet(LInspector inspector, bool apply) =>
        inspector.LInspectorCrop.LInspectorApplySet(apply);
    internal static LWorkVideoStep TInspectorStepRead(LInspector inspector, LColorKind kind) =>
        inspector.LInspectorStepRead(kind);
    internal static void TInspectorVideoApply(LInspector inspector, LWorkVideo video) =>
        inspector.LInspectorVideoApply(video);
    internal static bool TInspectorPersistentCheck(LInspector inspector) => inspector.LInspectorPersistentCheck();
    internal static bool TInspectorPersistentCheck(LInspector inspector, LColorKind kind) =>
        inspector.LInspectorPersistentCheck(kind);
    internal static void TInspectorPersistentSet(LInspector inspector, LColorKind kind, bool persistent) =>
        inspector.LInspectorPersistentSet(kind, persistent);
    internal static void TInspectorPersistentApply(LInspector inspector, LWorkVideo video) =>
        inspector.LInspectorPersistentApply(video);
    internal static LWorkVideo TInspectorPersistentRead(LInspector inspector) => inspector.LInspectorPersistentRead();
    internal static LRotateFlip TRotateCropResolve(LWorkCrop crop) => LRotateFlip.LRotateCropResolve(crop);
    internal static void TInspectorToolSet(LInspector inspector, bool armed) => inspector.LInspectorToolSet(armed);
    internal static void TInspectorCapableSet(LInspector inspector, bool crop, bool orientation) =>
        inspector.LInspectorCapableSet(crop, orientation);

    internal static void TInspectorSaveSuspend(LInspector inspector) => inspector.LInspectorSaveSuspend();
    internal static void TInspectorSaveResume(LInspector inspector) => inspector.LInspectorSaveResume();

    internal static LWorkAudioStep TInspectorStepRead(LInspectorAudio audio, LAudioKind kind) =>
        audio.LInspectorStepRead(kind);
    internal static void TInspectorAudioApply(LInspectorAudio audio, LWorkAudio plan) =>
        audio.LInspectorAudioApply(plan);
    internal static bool TInspectorPersistentCheck(LInspectorAudio audio) => audio.LInspectorPersistentCheck();
    internal static void TInspectorPersistentApply(LInspectorAudio audio, LWorkAudio plan, bool skipPersistent) =>
        audio.LInspectorPersistentApply(plan, skipPersistent);
    internal static LWorkAudio TInspectorPersistentRead(LInspectorAudio audio) => audio.LInspectorPersistentRead();
    internal static void TInspectorAudioAttach(LInspectorAudio audio, Action handler) =>
        audio.LInspectorAudioChanged += handler;

    internal static LInspectorChoice TInspectorChoiceRead(
        IReadOnlyList<string> tokens, Func<string, string> keyRead, string? token, string? match) =>
        LInspectorPlan.LInspectorChoiceRead(tokens, keyRead, token, match);
    internal static string? TInspectorChoiceResolve(
        IReadOnlyList<string> tokens, int index, string? token, string? match) =>
        LInspectorPlan.LInspectorChoiceResolve(tokens, index, token, match);
}
