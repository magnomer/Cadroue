using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceInspector
{
    internal static QInspector TInspectorCreate() => new(TInterface.TAtelierCreate().CAtelierInspectorCreate());
    internal static void TInspectorAttach(QInspector inspector, Action handler) =>
        inspector.QInspectorChanged += handler;
    internal static void TInspectorSourceSet(QInspector inspector, double width, double height) =>
        inspector.QInspectorSourceSet(width, height);
    internal static void TInspectorStepSet(QInspector inspector, string? step) => inspector.QInspectorStepSet(step);
    internal static void TInspectorMinimizedSet(QInspector inspector, bool minimized) =>
        inspector.QInspectorMinimizedSet(minimized);
    internal static bool TInspectorOwnerSet(QInspector inspector, string path) => inspector.QInspectorOwnerSet(path);
    internal static bool TInspectorFailureSet(QInspector inspector, string? path) =>
        inspector.QInspectorFailureSet(path);
    internal static LWorkCrop TInspectorCropRead(QInspector inspector) => inspector.QInspectorCrop.QInspectorCropRead();
    internal static LCropbox? TInspectorRectRead(QInspector inspector) => inspector.QInspectorCrop.QInspectorRectRead();
    internal static LRotateFlip TInspectorRotateRead(QInspector inspector) =>
        inspector.QInspectorOrientation.QInspectorRotateRead();
    internal static void TInspectorCropApply(QInspector inspector, LWorkCrop crop, bool apply) =>
        inspector.QInspectorCrop.QInspectorCropApply(crop, apply);
    internal static void TInspectorRatioApply(
        QInspector inspector, bool fixedRatio, bool lenient, int width, int height) =>
        inspector.QInspectorRatio.QInspectorRatioApply(fixedRatio, lenient, width, height);
    internal static void TInspectorCropReset(QInspector inspector) => inspector.QInspectorCrop.QInspectorCropReset();
    internal static void TInspectorPlanApply(QInspector inspector, LEditPlan plan) =>
        inspector.QInspectorPlanApply(plan);
    internal static void TInspectorVideoAttach(QInspector inspector, Action handler) =>
        inspector.QInspectorVideo.QInspectorVideoChanged += handler;
    internal static void TInspectorPersistentAttach(QInspector inspector, Action handler) =>
        inspector.QInspectorPersistentChanged += handler;
    internal static string TInspectorTitleRead(QInspector inspector) => inspector.QInspectorTitleRead();
    internal static bool TInspectorSectionCheck(QInspector inspector, string key) =>
        inspector.QInspectorSectionCheck(key);
    internal static double TInspectorValueCommit(string text, double current, double? least, double? most) =>
        CInspectorField.CInspectorValueCommit(text, current, least, most);
    internal static string TInspectorValueFormat(string text, double number, string format) =>
        CInspectorField.CInspectorValueFormat(text, number, format);
    internal static void TInspectorSlideCommit(
        double slider, double current, double least, double most, Action<double> set) =>
        CInspectorField.CInspectorSlideCommit(slider, current, least, most, set);
    internal static QInspectorTip TInspectorTipResolve(
        QInspector inspector, bool active, bool capable, bool preview, string disabled, string previewKey) =>
        QInspectorTip.QInspectorTipResolve(
            active, capable, preview, disabled, previewKey, "Inspector.Common.Apply", "Inspector.Common.Persistent");
    internal static void TInspectorPresetSelect(QInspector inspector, int index) =>
        inspector.QInspectorRatio.QInspectorPresetSelect(index);
    internal static void TInspectorFixedSet(QInspector inspector, bool fixedRatio) =>
        inspector.QInspectorRatio.QInspectorFixedSet(fixedRatio);
    internal static void TInspectorLenientSet(QInspector inspector, bool lenient) =>
        inspector.QInspectorRatio.QInspectorLenientSet(lenient);
    internal static void TInspectorRatioCommit(
        QInspector inspector, string width, string height, bool typed = true) =>
        inspector.QInspectorRatio.QInspectorRatioCommit(width, height, typed);
    internal static string TInspectorRatioFormat(QInspector inspector, bool width, string text) =>
        inspector.QInspectorRatio.QInspectorRatioFormat(width, text);
    internal static string TInspectorNoticeRead(QInspector inspector) =>
        inspector.QInspectorCrop.QInspectorNoticeRead();
    internal static string TInspectorResolutionRead(QInspector inspector) =>
        inspector.QInspectorCrop.QInspectorResolutionRead();
    internal static void TInspectorEdgeCommit(QInspector inspector, int side, string text, bool typed = true) =>
        inspector.QInspectorCrop.QInspectorEdgeCommit(side, text, typed);
    internal static string TInspectorEdgeFormat(QInspector inspector, int side, string text) =>
        inspector.QInspectorCrop.QInspectorEdgeFormat(side, text);
    internal static void TInspectorEdgeReset(QInspector inspector) => inspector.QInspectorCrop.QInspectorEdgeReset();
    internal static void TInspectorCropSet(
        QInspector inspector, LCropbox? drawn, int drive, int anchorX, int anchorY) =>
        inspector.QInspectorCrop.QInspectorCropSet(drawn, drive, anchorX, anchorY);
    internal static void TInspectorRotateSelect(QInspector inspector, int index) =>
        inspector.QInspectorOrientation.QInspectorRotateSelect(index);
    internal static void TInspectorFlipSet(QInspector inspector, bool horizontal, bool flipped) =>
        inspector.QInspectorOrientation.QInspectorFlipSet(horizontal, flipped);
    internal static void TInspectorApplySet(QInspector inspector, bool apply) =>
        inspector.QInspectorCrop.QInspectorApplySet(apply);
    internal static LWorkVideoStep TInspectorStepRead(QInspector inspector, LColorKind kind) =>
        inspector.QInspectorVideo.QInspectorStepRead(kind);
    internal static void TInspectorVideoApply(QInspector inspector, LWorkVideo video) =>
        inspector.QInspectorVideo.QInspectorVideoApply(video);
    internal static bool TInspectorPersistentCheck(QInspector inspector) =>
        inspector.QInspectorVideo.QInspectorPersistentCheck();
    internal static bool TInspectorPersistentCheck(QInspector inspector, LColorKind kind) =>
        inspector.QInspectorVideo.QInspectorPersistentCheck(kind);
    internal static void TInspectorPersistentSet(QInspector inspector, LColorKind kind, bool persistent) =>
        inspector.QInspectorVideo.QInspectorPersistentSet(kind, persistent);
    internal static void TInspectorPersistentApply(QInspector inspector, LWorkVideo video) =>
        inspector.QInspectorVideo.QInspectorPersistentApply(video);
    internal static LWorkVideo TInspectorPersistentRead(QInspector inspector) =>
        inspector.QInspectorVideo.QInspectorPersistentRead();
    internal static LRotateFlip TRotateCropResolve(LWorkCrop crop) => LRotateFlip.LRotateCropResolve(crop);
    internal static void TInspectorToolSet(QInspector inspector, bool armed) => inspector.QInspectorToolSet(armed);
    internal static void TInspectorCapableSet(QInspector inspector, bool crop, bool orientation) =>
        inspector.QInspectorCapableSet(crop, orientation);

    internal static void TInspectorSaveSuspend(QInspector inspector) => inspector.QInspectorSaveSuspend();
    internal static void TInspectorSaveResume(QInspector inspector) => inspector.QInspectorSaveResume();

    internal static LWorkAudioStep TInspectorStepRead(QInspectorAudio audio, LAudioKind kind) =>
        audio.QInspectorStepRead(kind);
    internal static void TInspectorAudioApply(QInspectorAudio audio, LWorkAudio plan) =>
        audio.QInspectorAudioApply(plan);
    internal static bool TInspectorPersistentCheck(QInspectorAudio audio) => audio.QInspectorPersistentCheck();
    internal static void TInspectorPersistentApply(QInspectorAudio audio, bool skipPersistent) =>
        audio.QInspectorPersistentApply(skipPersistent);
    internal static LWorkAudio TInspectorPersistentRead(QInspectorAudio audio) => audio.QInspectorPersistentRead();
    internal static void TInspectorAudioAttach(QInspectorAudio audio, Action handler) =>
        audio.QInspectorAudioChanged += handler;

    internal static QInspectorChoice TInspectorChoiceRead(
        IReadOnlyList<string> tokens, Func<string, string> keyRead, string? token, string? match) =>
        QInspectorPlan.QInspectorChoiceRead(tokens, keyRead, token, match);
    internal static string? TInspectorChoiceResolve(
        IReadOnlyList<string> tokens, int index, string? token, string? match) =>
        QInspectorPlan.QInspectorChoiceResolve(tokens, index, token, match);
}
