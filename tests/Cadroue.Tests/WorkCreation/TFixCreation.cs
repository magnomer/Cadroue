using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFixCreation
{
    [Fact]
    public void EmptyList_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = TFixCreate(Array.Empty<string>(), TFixOutputCreate());

        Assert.Empty(work);
    }

    [Fact]
    public void EverySelectedSource_ProducesOneFixWorkItem()
    {
        string first = Path.Combine("media", "first.mov");
        string second = Path.Combine("media", "second.mov");

        IReadOnlyList<LWorkItem> work = TFixCreate(new[] { first, second }, TFixOutputCreate());

        Assert.Equal(2, work.Count);
        Assert.All(work, item => Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind));
    }

    [Fact]
    public void CopyModePreset_IsAccepted()
    {
        LWorkItem item = Assert.Single(TFixCreate(new[] { "media/source.mov" }, TWorkOutput.TWorkSplitCreate()));

        Assert.Equal(LWorkKind.LWorkKindFix, item.LWorkKind);
    }

    [Fact]
    public void OutputAndSourceIdentity_ArePreserved()
    {
        string source = Path.Combine("incoming", "scene.mov");
        string folder = Path.Combine("exports", "approved");
        LEncoding output = TFixOutputCreate("{OriginalName}_fixed", "mkv", folder);

        LWorkItem item = Assert.Single(TFixCreate(new[] { source }, output, "fix-tab"));

        Assert.Equal(source, item.LWorkSourcePath);
        Assert.Equal("scene_fixed.mov", item.LWorkOutputName);
        Assert.Equal(Path.Combine(folder, "scene_fixed.mov"), item.LWorkOutputPath);
        Assert.Equal("fix-tab", item.LWorkTab);
        Assert.Same(output, item.LWorkOutput);
    }

    private static IReadOnlyList<LWorkItem> TFixCreate(
        IReadOnlyList<string> sources,
        LEncoding output,
        string tab = "test-tab") =>
        TInterface.TFixItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.TFixDescriptionCreate(sources, output),
            tab,
            _ => { },
            _ => TimeSpan.FromMinutes(3));

    private static LEncoding TFixOutputCreate(
        string pattern = "{OriginalName}_fix",
        string extension = "mp4",
        string? folder = null) => TWorkOutput.TWorkOutputCreate(pattern, extension, folder);
}
