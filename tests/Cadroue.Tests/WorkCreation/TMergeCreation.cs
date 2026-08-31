using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TMergeCreation : IDisposable
{
    private readonly string tMergeFolder = Path.Combine(Path.GetTempPath(), $"cadroue-merge-{Guid.NewGuid():N}");

    [Fact]
    public void OrderedInputs_RemainOrdered()
    {
        string first = TMergeFileCreate("first.mov");
        string second = TMergeFileCreate("second.mov");
        string third = TMergeFileCreate("third.mov");

        LWorkItem item = Assert.Single(TMergeCreate(new[] { first, second, third }, TWorkOutput.TWorkOutputCreate()));

        Assert.Equal(new[] { first, second, third }, item.LWorkMergeSources);
    }

    [Fact]
    public void SingleFileGroup_ProducesOneMergeWorkItem()
    {
        string only = TMergeFileCreate("only.mov");

        LWorkItem item = Assert.Single(TMergeCreate(new[] { only }, TWorkOutput.TWorkOutputCreate()));

        Assert.Equal(LWorkKind.LWorkKindMerge, item.LWorkKind);
        Assert.Equal(new[] { only }, item.LWorkMergeSources);
    }

    [Fact]
    public void GroupWithNoExistingFile_ProducesNoWork()
    {
        string missing = Path.Combine(tMergeFolder, "missing.mov");

        IReadOnlyList<LWorkItem> work = TMergeCreate(new[] { missing }, TWorkOutput.TWorkOutputCreate());

        Assert.Empty(work);
    }

    [Fact]
    public void OneMergeRequest_ProducesOneMergeWorkItem()
    {
        string first = TMergeFileCreate("part-1.mov");
        string second = TMergeFileCreate("part-2.mov");

        LWorkItem item = Assert.Single(TMergeCreate(new[] { first, second }, TWorkOutput.TWorkOutputCreate()));

        Assert.Equal(LWorkKind.LWorkKindMerge, item.LWorkKind);
    }

    [Fact]
    public void OutputDestinationAndEncodingSettings_ArePreserved()
    {
        string first = TMergeFileCreate("part-a.mov");
        string second = TMergeFileCreate("part-b.mov");
        string outputFolder = Path.Combine(tMergeFolder, "exports");
        LEncoding output = TWorkOutput.TWorkOutputCreate("{OriginalName}_joined", "mkv", outputFolder);

        LWorkItem item = Assert.Single(TMergeCreate(new[] { first, second }, output));

        Assert.Equal("Timeline_joined.mkv", item.LWorkOutputName);
        Assert.Equal(Path.Combine(outputFolder, "Timeline_joined.mkv"), item.LWorkOutputPath);
        Assert.Same(output, item.LWorkOutput);
        Assert.Equal(output.LEncodingVideo, item.LWorkOutput.LEncodingVideo);
        Assert.Equal(output.LEncodingAudio, item.LWorkOutput.LEncodingAudio);
    }

    public void Dispose()
    {
        if (Directory.Exists(tMergeFolder))
        {
            Directory.Delete(tMergeFolder, recursive: true);
        }
    }

    private IReadOnlyList<LWorkItem> TMergeCreate(IReadOnlyList<string> sources, LEncoding output) =>
        TInterface.TMergeItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            new[] { TInterface.TWorkGroupCreate("Timeline", sources) },
            output,
            "merge-tab",
            _ => { },
            _ => { });

    private string TMergeFileCreate(string name)
    {
        Directory.CreateDirectory(tMergeFolder);
        string path = Path.Combine(tMergeFolder, name);
        System.IO.File.WriteAllText(path, string.Empty);
        return path;
    }
}
