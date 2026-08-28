using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FixWorkCreationTests
{
    [Fact]
    public void EmptyList_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = Create(Array.Empty<string>(), Output());

        Assert.Empty(work);
    }

    [Fact]
    public void EverySelectedSource_ProducesOneFixWorkItem()
    {
        string first = Path.Combine("media", "first.mov");
        string second = Path.Combine("media", "second.mov");

        IReadOnlyList<LWorkItem> work = Create(new[] { first, second }, Output());

        Assert.Equal(2, work.Count);
        Assert.All(work, item => Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind));
    }

    [Fact]
    public void CopyModePreset_IsAccepted()
    {
        LWorkItem item = Assert.Single(Create(new[] { "media/source.mov" }, WorkCreationOutput.SplitCreate()));

        Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind);
    }

    [Fact]
    public void OutputAndSourceIdentity_ArePreserved()
    {
        string source = Path.Combine("incoming", "scene.mov");
        string folder = Path.Combine("exports", "approved");
        LEncoding output = Output("{OriginalName}_fixed", "mkv", folder);

        LWorkItem item = Assert.Single(Create(new[] { source }, output, "fix-tab"));

        Assert.Equal(source, item.LWorkSourcePath);
        Assert.Equal("scene_fixed.mov", item.LWorkOutputName);
        Assert.Equal(Path.Combine(folder, "scene_fixed.mov"), item.LWorkOutputPath);
        Assert.Equal("fix-tab", item.LWorkTab);
        Assert.Same(output, item.LWorkOutput);
    }

    private static IReadOnlyList<LWorkItem> Create(
        IReadOnlyList<string> sources,
        LEncoding output,
        string tab = "test-tab") =>
        TInterface.FixItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.FixDescriptionCreate(sources, output),
            tab,
            _ => { },
            _ => TimeSpan.FromMinutes(3));

    private static LEncoding Output(
        string pattern = "{OriginalName}_fix",
        string extension = "mp4",
        string? folder = null) => WorkCreationOutput.Create(pattern, extension, folder);
}
