using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeAudio
{
    [Fact]
    public void VolumeAndEqualizer_UseProductionOrderAndOmitZeroBands()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkVolumeCreate(true, 4.5),
            TInterface.TWorkEqualizerCreate(true, new[]
            {
                TInterface.TWorkBandCreate(1000, 3),
                TInterface.TWorkBandCreate(250, 0),
                TInterface.TWorkBandCreate(4000, -2)
            })
        });

        string filter = TEncodeFilterRead(audio);

        Assert.Equal(
            "volume=4.5dB,equalizer=f=1000:t=q:w=1:g=3,equalizer=f=4000:t=q:w=1:g=-2",
            filter);
        Assert.DoesNotContain("f=250", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void HighPassAndLowPass_EmitRequestedStageCounts()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkHighCreate(true, 120, 3, 2, 0.7),
            TInterface.TWorkLowCreate(true, 8000, 2, 1, 0.5)
        });

        string filter = TEncodeFilterRead(audio);

        Assert.Equal(3, filter.Split(',').Count(part => part.StartsWith("highpass=", StringComparison.Ordinal)));
        Assert.Equal(2, filter.Split(',').Count(part => part.StartsWith("lowpass=", StringComparison.Ordinal)));
        Assert.Contains("highpass=f=120:poles=2:width_type=q:width=0.7", filter, StringComparison.Ordinal);
        Assert.Contains("lowpass=f=8000:poles=1:width_type=q:width=0.5", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicNormalization_EmitsProductionFilterArguments()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkNormalizeCreate(
                true, LLeveling.LLevelingDynamic, -21, -2, 6, false, 300, 21, 10, 6)
        });

        Assert.Equal("dynaudnorm=f=300:g=21:m=10:p=0.95:s=6", TEncodeFilterRead(audio));
    }

    [Fact]
    public void TwoPassLoudness_EmitsAnalyzeAndMeasuredApplyStages()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkNormalizeCreate(
                true, LLeveling.LLevelingLoudness, -16, -1.5, 8, true, 300, 21, 10, 6)
        });
        LWorkItem work = TEncodeWorkCreate(audio);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TEncodeStagesBuild(work);
        LEncodeStage analyze = Assert.Single(stages, stage => stage.LEncodeStageMeasure);
        LEncodeStage process = Assert.Single(stages, stage => stage.LEncodeStageLabel == "Processing audio");

        Assert.Equal(
            "loudnorm=I=-16:TP=-1.5:LRA=8:print_format=json",
            TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(analyze.LEncodeStageArguments), "-af"));
        Assert.Equal(
            "loudnorm=I=-16:TP=-1.5:LRA=8@@MEASURED@@",
            TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(process.LEncodeStageArguments), "-af"));
        Assert.Contains("-f null -", analyze.LEncodeStageArguments, StringComparison.Ordinal);
    }

    private static string TEncodeFilterRead(LWorkAudio audio)
    {
        LWorkItem work = TEncodeWorkCreate(audio);
        LEncodeStage process = Assert.Single(
            TEncodeCommand.TEncodeStagesBuild(work), stage => stage.LEncodeStageLabel == "Processing audio");
        return TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(process.LEncodeStageArguments), "-af");
    }

    private static LWorkItem TEncodeWorkCreate(LWorkAudio audio) => TEncodeCommand.TWorkCreate(
        LWorkKind.LWorkKindAudio, Path.Combine("input media", "source audio.mov"),
        Path.Combine("output media", "processed audio.mkv"),
        TEncodeCommand.TOutputCreate(container: "mkv", extension: "mkv"),
        end: TimeSpan.FromMinutes(3), audio: audio);
}
