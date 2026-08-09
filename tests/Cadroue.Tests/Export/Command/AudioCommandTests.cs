using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class AudioCommandTests
{
    [Fact]
    public void VolumeAndEqualizer_UseProductionOrderAndOmitZeroBands()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkVolumeCreate(true, 4.5),
            TInterface.WorkEqualizerCreate(true, new[]
            {
                TInterface.WorkBandCreate(1000, 3),
                TInterface.WorkBandCreate(250, 0),
                TInterface.WorkBandCreate(4000, -2)
            })
        });

        string filter = ProcessingFilter(audio);

        Assert.Equal(
            "volume=4.5dB,equalizer=f=1000:t=q:w=1:g=3,equalizer=f=4000:t=q:w=1:g=-2",
            filter);
        Assert.DoesNotContain("f=250", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void HighPassAndLowPass_EmitRequestedStageCounts()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkHighCreate(true, 120, 3, 2, 0.7),
            TInterface.WorkLowCreate(true, 8000, 2, 1, 0.5)
        });

        string filter = ProcessingFilter(audio);

        Assert.Equal(3, filter.Split(',').Count(part => part.StartsWith("highpass=", StringComparison.Ordinal)));
        Assert.Equal(2, filter.Split(',').Count(part => part.StartsWith("lowpass=", StringComparison.Ordinal)));
        Assert.Contains("highpass=f=120:poles=2:width_type=q:width=0.7", filter, StringComparison.Ordinal);
        Assert.Contains("lowpass=f=8000:poles=1:width_type=q:width=0.5", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicNormalization_EmitsProductionFilterArguments()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkNormalizeCreate(
                true, LLeveling.LLevelingDynamic, -21, -2, 6, false, 300, 21, 10, 6)
        });

        Assert.Equal("dynaudnorm=f=300:g=21:m=10:p=0.95:s=6", ProcessingFilter(audio));
    }

    [Fact]
    public void TwoPassLoudness_EmitsAnalyzeAndMeasuredApplyStages()
    {
        using var environment = new TEncodeCommand();
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkNormalizeCreate(
                true, LLeveling.LLevelingLoudness, -16, -1.5, 8, true, 300, 21, 10, 6)
        });
        LWorkItem work = AudioWork(audio);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.StagesBuild(work);
        LEncodeStage analyze = Assert.Single(stages, stage => stage.LEncodeStageMeasure);
        LEncodeStage process = Assert.Single(stages, stage => stage.LEncodeStageLabel == "Processing audio");

        Assert.Equal(
            "loudnorm=I=-16:TP=-1.5:LRA=8:print_format=json",
            CommandTokens.ValueAfter(CommandTokens.Read(analyze.LEncodeStageArguments), "-af"));
        Assert.Equal(
            "loudnorm=I=-16:TP=-1.5:LRA=8@@MEASURED@@",
            CommandTokens.ValueAfter(CommandTokens.Read(process.LEncodeStageArguments), "-af"));
        Assert.Contains("-f null -", analyze.LEncodeStageArguments, StringComparison.Ordinal);
    }

    private static string ProcessingFilter(LWorkAudio audio)
    {
        LWorkItem work = AudioWork(audio);
        LEncodeStage process = Assert.Single(
            TEncodeCommand.StagesBuild(work), stage => stage.LEncodeStageLabel == "Processing audio");
        return CommandTokens.ValueAfter(CommandTokens.Read(process.LEncodeStageArguments), "-af");
    }

    private static LWorkItem AudioWork(LWorkAudio audio) => TEncodeCommand.WorkCreate(
        LWorkKind.LWorkKindAudio, Path.Combine("input media", "source audio.mov"),
        Path.Combine("output media", "processed audio.mkv"),
        TEncodeCommand.OutputCreate(container: "mkv", extension: "mkv"),
        end: TimeSpan.FromMinutes(3), audio: audio);
}
