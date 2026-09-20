using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSourceFilter
{
    [Fact]
    public void Filter_ListsVideoPatterns_AudioOnlyWhenAllowed()
    {
        LViewer viewer = TInterface.TViewerCreate();

        string video = TInterface.TSourceFilterRead(TInterface.TSourceCreate(viewer, false));
        string media = TInterface.TSourceFilterRead(TInterface.TSourceCreate(viewer, true));

        Assert.False(string.IsNullOrWhiteSpace(video));
        Assert.False(string.IsNullOrWhiteSpace(media));
        Assert.NotEqual(video, media);
    }

    [Fact]
    public void Placeholder_ShownForBlankText()
    {
        Assert.True(TInterface.TSourcePlaceholderCheck(""));
        Assert.True(TInterface.TSourcePlaceholderCheck("   "));
        Assert.False(TInterface.TSourcePlaceholderCheck(@"C:\clip.mp4"));
    }

    [Fact]
    public void MediaChange_PublishesThePathToShow()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LSource source = TInterface.TSourceCreate(viewer, true);
        List<string> paths = [];
        TInterface.TSourcePathAttach(source, paths.Add);

        TInterface.TViewerMediaRaise(viewer, TInterface.TCargoCreate(@"C:\clip.mp4", null, false));

        Assert.Equal([@"C:\clip.mp4"], paths);
        Assert.Equal(@"C:\clip.mp4", source.LSourcePath);
    }

    [Fact]
    public void Open_RefusesAudioWhenNotAllowed_KeyRunNeedsReturnAndAnExistingFile()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LSource source = TInterface.TSourceCreate(viewer, false);
        List<string> refusals = [];
        TInterface.TSourceRefuseAttach(source, (title, message) => refusals.Add(title));

        Assert.False(TInterface.TSourceOpen(source, @"C:\song.flac"));
        Assert.Single(refusals);
        Assert.False(TInterface.TSourceKeyRun(source, "Escape", @"C:\song.flac"));
        string missing = @"C:\missing-" + Guid.NewGuid().ToString("N") + ".mp4";
        Assert.False(TInterface.TSourceKeyRun(source, "Return", missing));
        Assert.Single(refusals);

        TInterface.TSourceDialogOpen(source, false, @"C:\song.flac");
        TInterface.TSourceDialogOpen(source, null, @"C:\song.flac");
        Assert.Single(refusals);
        TInterface.TSourceDialogOpen(source, true, @"C:\song.flac");
        Assert.Equal(2, refusals.Count);
    }
}
