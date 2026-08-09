using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class AudioWorkCreationTests
{
    [Fact]
    public void ValidAudioProcessing_CreatesAudioWork()
    {
        LWorkItem item = Assert.IsType<LWorkItem>(Create("media/source.mov", Processing(), WorkCreationOutput.Create()));

        Assert.Equal(LWorkKind.LWorkKindAudio, item.LWorkKind);
    }

    [Fact]
    public void AudioSettings_SurviveConstruction()
    {
        LWorkAudio processing = Processing();

        LWorkItem item = Assert.IsType<LWorkItem>(Create("media/source.mov", processing, WorkCreationOutput.Create()));

        Assert.Same(processing, item.LWorkAudio);
        LWorkVolumeStep volume = Assert.IsType<LWorkVolumeStep>(item.LWorkAudio.LWorkAudioSteps[0]);
        Assert.True(volume.LWorkStepActive);
        Assert.Equal(7.5, volume.LWorkVolumeGain);
    }

    [Fact]
    public void SourceOutputAndRequestIdentity_ArePreserved()
    {
        string source = Path.Combine("incoming", "interview.mov");
        string folder = Path.Combine("exports", "audio-fixed");
        Guid batchId = Guid.NewGuid();
        LEncoding output = WorkCreationOutput.Create(extension: "mkv", folder: folder);

        LWorkItem item = Assert.IsType<LWorkItem>(Create(
            source,
            Processing(),
            output,
            LWorkPriority.LWorkPriorityHigh,
            "audio-tab",
            batchId));

        Assert.Equal(source, item.LWorkSourcePath);
        Assert.Equal(Path.Combine(folder, "interview.mkv"), item.LWorkOutputPath);
        Assert.Same(output, item.LWorkOutput);
        Assert.Equal(LWorkPriority.LWorkPriorityHigh, item.LWorkPriority);
        Assert.Equal("audio-tab", item.LWorkTab);
        Assert.Equal(batchId, item.LWorkBatchId);
    }

    private static LWorkItem? Create(
        string? source,
        LWorkAudio processing,
        LEncoding output,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal,
        string tab = "test-tab",
        Guid batchId = default) =>
        TInterface.AudioItemCreate(
            priority,
            source,
            processing,
            output,
            tab,
            _ => { },
            _ => { },
            _ => TimeSpan.FromMinutes(4),
            batchId);

    private static LWorkAudio Processing() => TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkVolumeCreate(true, 7.5),
            TInterface.WorkHighCreate(true, 120, 2, 2, 0.7)
        });
}
