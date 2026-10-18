namespace Cadroue.UIDeportment;

public static class QAudioProcessing
{
    public static readonly IReadOnlyList<QProcessingRow> QAudioRows =
    [
        new("High Pass", "/PAsset/PPanel/PProcessingHighPass.svg", "Processing.Step.HighPass"),
        new("Low Pass", "/PAsset/PPanel/PProcessingLowPass.svg", "Processing.Step.LowPass"),
        new("Noise Reduction", "/PAsset/PPanel/PProcessingNoiseReduction.svg", "Processing.Step.NoiseReduction"),
        new("Equalizer", "/PAsset/PPanel/PProcessingEqualizer.svg", "Processing.Step.Equalizer"),
        new("Volume", "/PAsset/PPanel/PProcessingVolume.svg", "Processing.Step.Volume"),
        new("Normalize", "/PAsset/PPanel/PProcessingNormalize.svg", "Processing.Step.Normalize"),
    ];
}
