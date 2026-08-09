using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class MergeWorkCreationTests : IDisposable
{
    private readonly string testFolder = Path.Combine(Path.GetTempPath(), $"cadroue-merge-{Guid.NewGuid():N}");

    [Fact]
    public void OrderedInputs_RemainOrdered()
    {
        string first = File("first.mov");
        string second = File("second.mov");
        string third = File("third.mov");

        LWorkItem item = Assert.Single(Create(new[] { first, second, third }, WorkCreationOutput.Create()));

        Assert.Equal(new[] { first, second, third }, item.LWorkMergeSources);
    }

    [Fact]
    public void SingleFileGroup_ProducesOneMergeWorkItem()
    {
        string only = File("only.mov");

        LWorkItem item = Assert.Single(Create(new[] { only }, WorkCreationOutput.Create()));

        Assert.Equal(LWorkKind.LWorkKindMerge, item.LWorkKind);
        Assert.Equal(new[] { only }, item.LWorkMergeSources);
    }

    [Fact]
    public void GroupWithNoExistingFile_ProducesNoWork()
    {
        string missing = Path.Combine(testFolder, "missing.mov");

        IReadOnlyList<LWorkItem> work = Create(new[] { missing }, WorkCreationOutput.Create());

        Assert.Empty(work);
    }

    [Fact]
    public void OneMergeRequest_ProducesOneMergeWorkItem()
    {
        string first = File("part-1.mov");
        string second = File("part-2.mov");

        LWorkItem item = Assert.Single(Create(new[] { first, second }, WorkCreationOutput.Create()));

        Assert.Equal(LWorkKind.LWorkKindMerge, item.LWorkKind);
    }

    [Fact]
    public void OutputDestinationAndEncodingSettings_ArePreserved()
    {
        string first = File("part-a.mov");
        string second = File("part-b.mov");
        string outputFolder = Path.Combine(testFolder, "exports");
        LEncoding output = WorkCreationOutput.Create("{OriginalName}_joined", "mkv", outputFolder);

        LWorkItem item = Assert.Single(Create(new[] { first, second }, output));

        Assert.Equal("Timeline_joined.mkv", item.LWorkOutputName);
        Assert.Equal(Path.Combine(outputFolder, "Timeline_joined.mkv"), item.LWorkOutputPath);
        Assert.Same(output, item.LWorkOutput);
        Assert.Equal(output.LEncodingVideo, item.LWorkOutput.LEncodingVideo);
        Assert.Equal(output.LEncodingAudio, item.LWorkOutput.LEncodingAudio);
    }

    public void Dispose()
    {
        if (Directory.Exists(testFolder))
        {
            Directory.Delete(testFolder, recursive: true);
        }
    }

    private IReadOnlyList<LWorkItem> Create(IReadOnlyList<string> sources, LEncoding output) =>
        TInterface.MergeItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            new[] { TInterface.WorkGroupCreate("Timeline", sources) },
            output,
            "merge-tab",
            _ => { },
            _ => { });

    private string File(string name)
    {
        Directory.CreateDirectory(testFolder);
        string path = Path.Combine(testFolder, name);
        System.IO.File.WriteAllText(path, string.Empty);
        return path;
    }
}
