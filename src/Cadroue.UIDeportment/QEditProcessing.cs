namespace Cadroue.UIDeportment;

public static class QEditProcessing
{
    public static readonly IReadOnlyList<LProcessingRow> LEditRows =
    [
        new("Crop", "/PAsset/PPanel/PProcessingCrop.svg", "Processing.Step.Crop"),
        new("Whitebalance", "/PAsset/PPanel/PProcessingWhitebalance.svg", "Processing.Step.Whitebalance"),
        new("Exposure", "/PAsset/PPanel/PProcessingExposure.svg", "Processing.Step.Exposure"),
        new("Brightness", "/PAsset/PPanel/PProcessingBrightness.svg", "Processing.Step.Brightness"),
        new("Contrast", "/PAsset/PPanel/PProcessingContrast.svg", "Processing.Step.Contrast"),
        new("Gamma", "/PAsset/PPanel/PProcessingGamma.svg", "Processing.Step.Gamma"),
        new("Saturation", "/PAsset/PPanel/PProcessingSaturation.svg", "Processing.Step.Saturation"),
        new("Curve", "/PAsset/PPanel/PProcessingCurve.svg", "Processing.Step.Curve"),
    ];
}
