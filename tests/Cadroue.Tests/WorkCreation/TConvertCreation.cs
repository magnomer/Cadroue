using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TConvertCreation
{
    [Fact]
    public void EverySelectedSource_ProducesOneConvertWorkItem()
    {
        string first = Path.Combine("media", "first.mov");
        string second = Path.Combine("media", "second.mov");

        IReadOnlyList<LWorkItem> work = TConvertCreate(new[] { first, second }, TWorkOutput.TWorkOutputCreate());

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
            [selected] = TConvertMediaCreate(10),
            [unselected] = TConvertMediaCreate(20)
        };

        LWorkItem item = Assert.Single(TConvertCreate(new[] { selected }, TWorkOutput.TWorkOutputCreate(), media));

        Assert.Equal(selected, item.LWorkSourcePath);
        Assert.DoesNotContain(item.LWorkSourcePath, new[] { unselected });
    }

    [Fact]
    public void OutputEncodingSettings_ArePreserved()
    {
        LEncoding output = TWorkOutput.TWorkOutputCreate("{OriginalName}_web", "webm", Path.Combine("exports", "web"));

        LWorkItem item = Assert.Single(TConvertCreate(new[] { "media/source.mov" }, output));

        Assert.Same(output, item.LWorkOutput);
        Assert.Equal(output.LEncodingVideo, item.LWorkOutput.LEncodingVideo);
        Assert.Equal(output.LEncodingAudio, item.LWorkOutput.LEncodingAudio);
    }

    [Fact]
    public void DistinctInputs_RetainDistinctSourceIdentities()
    {
        string first = Path.Combine("incoming", "camera-a.mov");
        string second = Path.Combine("incoming", "camera-b.mov");

        IReadOnlyList<LWorkItem> work = TConvertCreate(new[] { first, second }, TWorkOutput.TWorkOutputCreate());

        Assert.Equal(new[] { first, second }, work.Select(item => item.LWorkSourcePath));
        Assert.NotEqual(work[0].LWorkSourcePath, work[1].LWorkSourcePath);
    }

    [Fact]
    public void EqualStemsInOneOutputFolder_TakeDistinctOutputPaths()
    {
        string first = Path.Combine("camera-a", "clip.mov");
        string second = Path.Combine("camera-b", "clip.mov");
        LEncoding output = TWorkOutput.TWorkOutputCreate(folder: Path.Combine("exports", "web"));

        IReadOnlyList<LWorkItem> work = TConvertCreate(new[] { first, second }, output);

        Assert.Equal(2, work.Count);
        Assert.NotEqual(work[0].LWorkOutputPath, work[1].LWorkOutputPath);
        Assert.Equal("clip.mp4", work[0].LWorkOutputName);
        Assert.Equal("clip_2.mp4", work[1].LWorkOutputName);
    }

    [Fact]
    public void ConstantNamePattern_TakesDistinctOutputPaths()
    {
        LEncoding output = TWorkOutput.TWorkOutputCreate("archive", folder: "exports");

        IReadOnlyList<LWorkItem> work = TConvertCreate(
            new[] { Path.Combine("media", "first.mov"), Path.Combine("media", "second.mov") }, output);

        Assert.Equal(new[] { "archive.mp4", "archive_2.mp4" }, work.Select(item => item.LWorkOutputName));
    }

    [Fact]
    public void EqualStemsInTheirOwnSourceFolders_KeepOneName()
    {
        IReadOnlyList<LWorkItem> work = TConvertCreate(
            new[] { Path.Combine("camera-a", "clip.mov"), Path.Combine("camera-b", "clip.mov") },
            TWorkOutput.TWorkOutputCreate());

        Assert.All(work, item => Assert.Equal("clip.mp4", item.LWorkOutputName));
        Assert.NotEqual(work[0].LWorkOutputPath, work[1].LWorkOutputPath);
    }

    [Fact]
    public void TokenTextInTheSourceStem_IsKeptLiterally()
    {
        LWorkItem item = Assert.Single(TConvertCreate(
            new[] { Path.Combine("media", "clip {Date}.mov") }, TWorkOutput.TWorkOutputCreate()));

        Assert.Equal("clip {Date}.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void EditOperatorTextInTheSourceStem_IsKeptLiterally()
    {
        LWorkItem item = Assert.Single(TConvertCreate(
            new[] { Path.Combine("media", "clip{Backspace:4}.mov") }, TWorkOutput.TWorkOutputCreate()));

        Assert.Equal("clip{Backspace_4}.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void EditOperatorInTheNamePattern_StillTrimsTheStem()
    {
        LEncoding output = TWorkOutput.TWorkOutputCreate("{OriginalName}{Backspace:4}");

        LWorkItem item = Assert.Single(TConvertCreate(new[] { Path.Combine("media", "recording.mov") }, output));

        Assert.Equal("recor.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void DurationTokens_ReadTheResolvedSourceDuration()
    {
        LEncoding output = TWorkOutput.TWorkOutputCreate("{OriginalName}_{SectionDuration}");

        LWorkItem item = Assert.Single(TConvertCreate(new[] { Path.Combine("media", "clip.mov") }, output));

        Assert.Equal("clip_00-01-00.000.mp4", item.LWorkOutputName);
    }

    private static IReadOnlyList<LWorkItem> TConvertCreate(
        IReadOnlyList<string> sources,
        LEncoding output,
        IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        TInterface.TConvertItemsCreate(
            LWorkPriority.LWorkPriorityNormal,
            TInterface.TConvertDescriptionCreate(sources, output, media),
            "convert-tab",
            _ => { },
            _ => TimeSpan.FromMinutes(1));

    private static LWorkMedia TConvertMediaCreate(long seconds) =>
        TInterface.TWorkMediaCreate(1920, 1080, 30, seconds * 1000, true);
}
