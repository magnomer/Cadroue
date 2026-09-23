using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEditCreation
{
    [Fact]
    public void MissingSource_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = TEditCreate(
            null,
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(),
            TEditOutputCreate());

        Assert.Empty(work);
    }

    [Fact]
    public void ValidEditSettings_SurviveIntoWorkRequest()
    {
        LWorkCrop crop = TInterface.TWorkCropCreate(11, 12, 13, 14, 90, true, false);
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkBrightnessCreate(true, 37),
            TInterface.TWorkContrastCreate(true, 125)
        });

        LWorkItem item = Assert.Single(TEditCreate("media/source.mov", crop, video, TEditOutputCreate()));

        Assert.Equal(LWorkKind.LWorkKindEdit, item.LWorkKind);
        Assert.Same(crop, item.LWorkCrop);
        Assert.Same(video, item.LWorkVideo);
    }

    [Fact]
    public void OutputAndRequestIdentity_ArePreserved()
    {
        string source = Path.Combine("incoming", "scene.mov");
        string folder = Path.Combine("exports", "approved");
        Guid batchId = Guid.NewGuid();
        LEncoding output = TEditOutputCreate("{OriginalName}_graded", "mkv", folder);

        LWorkItem item = Assert.Single(TEditCreate(
            source,
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(),
            output,
            LWorkPriority.LWorkPriorityHigh,
            "edit-tab",
            batchId));

        Assert.Equal(source, item.LWorkSourcePath);
        Assert.Equal("scene_graded.mkv", item.LWorkOutputName);
        Assert.Equal(Path.Combine(folder, "scene_graded.mkv"), item.LWorkOutputPath);
        Assert.Equal(LWorkPriority.LWorkPriorityHigh, item.LWorkPriority);
        Assert.Equal("edit-tab", item.LWorkTab);
        Assert.Equal(batchId, item.LWorkBatchId);
        Assert.Same(output, item.LWorkOutput);
    }

    [Fact]
    public void ListSourceWithoutSavedPlan_QueuesPlainEdit()
    {
        Guid looseBatch = Guid.NewGuid();

        LWorkItem item = Assert.Single(TInterface.TEditSourcesCreate(
            new[] { ("media/plain.mov", Guid.Empty) },
            TEditOutputCreate(),
            _ => null,
            looseBatch));

        Assert.Equal("media/plain.mov", item.LWorkSourcePath);
        Assert.False(item.LWorkCrop.LWorkCropActive);
        Assert.False(item.LWorkVideo.LWorkVideoActive);
        Assert.Equal(looseBatch, item.LWorkBatchId);
    }

    [Fact]
    public void ListSourceWithApplyOnButNoSettings_QueuesPlainEdit()
    {
        LEditPlan plan = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(),
            TInterface.TWorkVideoCreate(new[] { TInterface.TWorkBrightnessCreate(true, 0) }),
            true);
        LSidecarEditRecord record = TInterface.TEditPersistentCreate(plan);
        Guid sourceBatch = Guid.NewGuid();

        LWorkItem item = Assert.Single(TInterface.TEditSourcesCreate(
            new[] { ("media/apply.mov", sourceBatch) },
            TEditOutputCreate(),
            _ => record,
            Guid.NewGuid()));

        Assert.False(item.LWorkCrop.LWorkCropActive);
        Assert.Equal(sourceBatch, item.LWorkBatchId);
    }

    [Fact]
    public void ListSources_EachQueuedWhetherPlanIsSavedOrNot()
    {
        LSidecarEditRecord record = TInterface.TEditPersistentCreate(TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false),
            TInterface.TWorkVideoCreate(),
            true));

        IReadOnlyList<LWorkItem> work = TInterface.TEditSourcesCreate(
            new[] { ("media/cropped.mov", Guid.Empty), ("media/untouched.mov", Guid.Empty) },
            TEditOutputCreate(),
            path => path == "media/cropped.mov" ? record : null,
            Guid.NewGuid());

        Assert.Equal(2, work.Count);
        Assert.True(work[0].LWorkCrop.LWorkCropActive);
        Assert.False(work[1].LWorkCrop.LWorkCropActive);
    }

    private static IReadOnlyList<LWorkItem> TEditCreate(
        string? source,
        LWorkCrop crop,
        LWorkVideo video,
        LEncoding output,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal,
        string tab = "test-tab",
        Guid batchId = default) =>
        TInterface.TEditItemsCreate(
            priority,
            TInterface.TEditDescriptionCreate(source, TimeSpan.FromMinutes(3), crop, video, output),
            tab,
            _ => { },
            _ => { },
            batchId);

    private static LEncoding TEditOutputCreate(
        string pattern = "{OriginalName}_edit",
        string extension = "mp4",
        string? folder = null) => TWorkOutput.TWorkOutputCreate(pattern, extension, folder);
}
