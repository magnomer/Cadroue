using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class ConvertWorkCreationTests
{
    [Fact]
    public void EverySelectedSource_ProducesOneConvertWorkItem()
    {
        string first = Path.Combine("media", "first.mov");
        string second = Path.Combine("media", "second.mov");

        IReadOnlyList<LWorkItem> work = Create(new[] { first, second }, WorkCreationOutput.Create());

        Assert.Equal(2, work.Count);
        Assert.All(work, item => Assert.Equal(LWorkKind.LWorkKindConvert, item.LWorkKind));
    }

    [Fact]
    public void UnselectedSources_AreNotAdded()
    {
        string selected = Path.Combine("media", "selected.mov");
        string unselected = Path.Combine("media", "unselected.mov");
        var media = new Dictionary<string, LWorkMedia>
        {
            [selected] = Media(10),
            [unselected] = Media(20)
        };

        LWorkItem item = Assert.Single(Create(new[] { selected }, WorkCreationOutput.Create(), media));

        Assert.Equal(selected, item.LWorkSourcePath);
        Assert.DoesNotContain(item.LWorkSourcePath, new[] { unselected });
    }

    [Fact]
    public void OutputEncodingSettings_ArePreserved()
    {
        LEncoding output = WorkCreationOutput.Create("{OriginalName}_web", "webm", Path.Combine("exports", "web"));

        LWorkItem item = Assert.Single(Create(new[] { "media/source.mov" }, output));

        Assert.Same(output, item.LWorkOutput);
        Assert.Equal(output.LEncodingVideo, item.LWorkOutput.LEncodingVideo);
        Assert.Equal(output.LEncodingAudio, item.LWorkOutput.LEncodingAudio);
    }

    [Fact]
    public void DistinctInputs_RetainDistinctSourceIdentities()
    {
        string first = Path.Combine("incoming", "camera-a.mov");
        string second = Path.Combine("incoming", "camera-b.mov");

        IReadOnlyList<LWorkItem> work = Create(new[] { first, second }, WorkCreationOutput.Create());

        Assert.Equal(new[] { first, second }, work.Select(item => item.LWorkSourcePath));
        Assert.NotEqual(work[0].LWorkSourcePath, work[1].LWorkSourcePath);
    }

    private static IReadOnlyList<LWorkItem> Create(
        IReadOnlyList<string> sources,
        LEncoding output,
        IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        TInterface.ConvertItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.ConvertDescriptionCreate(sources, output, media),
            "convert-tab",
            _ => { },
            _ => TimeSpan.FromMinutes(1));

    private static LWorkMedia Media(long seconds) => TInterface.WorkMediaCreate(1920, 1080, 30, seconds * 1000, true);
}
