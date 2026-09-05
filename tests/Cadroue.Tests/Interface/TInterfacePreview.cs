using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

/// <summary>
/// Test-side boundary for preview state: colour, gamma, crop, rotation and the edit plan.
/// </summary>
internal static partial class TInterface
{
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

}
