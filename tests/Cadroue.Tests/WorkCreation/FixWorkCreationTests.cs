using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FixWorkCreationTests
{
    [Fact]
    public void MissingSource_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = Create(null, TInterface.WorkCropCreate(), TInterface.WorkVideoCreate(), Output());

        Assert.Empty(work);
    }

    [Fact]
    public void ValidFixSettings_SurviveIntoWorkRequest()
    {
        LWorkCrop crop = TInterface.WorkCropCreate(11, 12, 13, 14, 90, true, false);
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkBrightnessCreate(true, 37),
            TInterface.WorkContrastCreate(true, 125)
        });

        LWorkItem item = Assert.Single(Create("media/source.mov", crop, video, Output()));

        Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind);
        Assert.Same(crop, item.LWorkCrop);
        Assert.Same(video, item.LWorkVideo);
    }

    [Fact]
    public void CopyModePreset_IsAccepted()
    {
        LWorkItem item = Assert.Single(Create(
            "media/source.mov",
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(),
            WorkCreationOutput.SplitCreate()));

        Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind);
    }

    [Fact]
    public void OutputAndRequestIdentity_ArePreserved()
    {
        string source = Path.Combine("incoming", "scene.mov");
        string folder = Path.Combine("exports", "approved");
        Guid batchId = Guid.NewGuid();
        LEncoding output = Output("{OriginalName}_graded", "mkv", folder);

        LWorkItem item = Assert.Single(Create(
            source,
            TInterface.WorkCropCreate(),
            TInterface.WorkVideoCreate(),
            output,
            LWorkPriority.LWorkPriorityHigh,
            "fix-tab",
            batchId));

        Assert.Equal(source, item.LWorkSourcePath);
        Assert.Equal("scene_graded.mkv", item.LWorkOutputName);
        Assert.Equal(Path.Combine(folder, "scene_graded.mkv"), item.LWorkOutputPath);
        Assert.Equal(LWorkPriority.LWorkPriorityHigh, item.LWorkPriority);
        Assert.Equal("fix-tab", item.LWorkTab);
        Assert.Equal(batchId, item.LWorkBatchId);
        Assert.Same(output, item.LWorkOutput);
    }

    private static IReadOnlyList<LWorkItem> Create(
        string? source,
        LWorkCrop crop,
        LWorkVideo video,
        LEncoding output,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal,
        string tab = "test-tab",
        Guid batchId = default) =>
        TInterface.FixItemsCreate(
            priority,
            TInterface.FixDescriptionCreate(source, TimeSpan.FromMinutes(3), crop, video, output),
            tab,
            _ => { },
            _ => { },
            batchId);

    private static LEncoding Output(
        string pattern = "{OriginalName}_fix",
        string extension = "mp4",
        string? folder = null) => WorkCreationOutput.Create(pattern, extension, folder);
}
