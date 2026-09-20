using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TViewerSource
{
    private static (LViewer TViewer, LViewerSource TSource) TSourceBuild()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);
        return (viewer, TInterface.TViewerSourceRead(viewer));
    }

    private static string TSourceFileCreate(string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-source-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, [0]);
        return path;
    }

    [Fact]
    public void SourceOpen_CommandInactive_Refuses()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        TInterface.TViewerCommandSet(viewer, false);

        TInterface.TViewerSourceOpen(source, "clip.mp4");

        Assert.Null(viewer.LViewerIntent);
        Assert.False(TInterface.TViewerSourceMatch(viewer, "clip.mp4"));
    }

    [Fact]
    public void SourceOpen_PlainPath_StartsLoad_AndReportsMissingFile()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterface.TViewerMediaAttach(viewer, cargos.Add);
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-open-{Guid.NewGuid():N}.mp4");

        TInterface.TViewerSourceOpen(source, path);

        Assert.Single(cargos);
        Assert.Equal(path, cargos[0].LCargoSourcePath);
        Assert.NotNull(cargos[0].LCargoFfmpegError);
        Assert.Null(viewer.LViewerIntent);
    }

    [Fact]
    public void SourceOpen_SidecarUnresolved_AsksWarning_AndRefuses()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        List<LViewerAskKind> asks = [];
        TInterface.TViewerAskAttach(source, (ask, answer) =>
        {
            asks.Add(ask.LViewerAskKind);
            answer(true);
        });
        TInterface.TViewerLibrarianAttach(path => path.EndsWith(".cad", StringComparison.Ordinal), _ => null, null);
        try
        {
            TInterface.TViewerSourceOpen(source, "clip.cad");
        }
        finally
        {
            TInterface.TViewerLibrarianAttach(null, null, null);
        }

        Assert.Equal([LViewerAskKind.LViewerAskWarning], asks);
        Assert.Null(viewer.LViewerIntent);
    }

    [Fact]
    public void SourceOpen_SidecarMismatch_DecisionAccepted_OpensResolvedPath()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterface.TViewerMediaAttach(viewer, cargos.Add);
        List<LViewerAskKind> asks = [];
        TInterface.TViewerAskAttach(source, (ask, answer) =>
        {
            asks.Add(ask.LViewerAskKind);
            answer(true);
        });
        string resolved = Path.Combine(Path.GetTempPath(), $"cadroue-resolved-{Guid.NewGuid():N}.mp4");
        TInterface.TViewerLibrarianAttach(
            path => path.EndsWith(".cad", StringComparison.Ordinal),
            _ => TInterface.TSidecarSourceCreate(resolved, LSidecarSourceKind.LSidecarSourceSibling),
            null);
        try
        {
            TInterface.TViewerSourceOpen(source, "clip.cad");
        }
        finally
        {
            TInterface.TViewerLibrarianAttach(null, null, null);
        }

        Assert.Equal([LViewerAskKind.LViewerAskDecision], asks);
        Assert.Single(cargos);
        Assert.Equal(resolved, cargos[0].LCargoSourcePath);
    }

    [Fact]
    public void PathHandle_SameSource_DoesNotReopen_OtherSourceDoes()
    {
        (LViewer viewer, _) = TSourceBuild();
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-same-{Guid.NewGuid():N}.mp4");
        TInterface.TViewerRequestSet(viewer, path);
        TInterface.TViewerIntentSet(viewer, path, TimeSpan.Zero, null);
        int serial = viewer.LViewerLoadSerial;

        TInterface.TViewerPathHandle(viewer, path);
        Assert.Equal(serial, viewer.LViewerLoadSerial);

        TInterface.TViewerPathHandle(viewer, Path.Combine(Path.GetTempPath(), "cadroue-other.mp4"));
        Assert.NotEqual(serial, viewer.LViewerLoadSerial);
    }

    [Fact]
    public void DropResolve_ListPresent_AcceptsFoldersAndMedia()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        TInterface.TViewerDropAttach(source, _ => { });
        string folder = Path.GetTempPath();

        Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TViewerDropResolve(source, [folder], true));
        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TViewerDropResolve(source, [folder], false));
        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TViewerDropResolve(source, null, true));
        Assert.Equal(
            LWindowDropEffect.LWindowDropNone,
            TInterface.TViewerDropResolve(source, [Path.Combine(folder, $"missing-{Guid.NewGuid():N}.txt")], true));
        Assert.Null(viewer.LViewerIntent);
    }

    [Fact]
    public void DropHandle_ListPresent_HandsPathsToList()
    {
        (_, LViewerSource source) = TSourceBuild();
        List<string> handed = [];
        TInterface.TViewerDropAttach(source, paths => handed.AddRange(paths));
        string folder = Path.GetTempPath();

        Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TViewerDropHandle(source, [folder], true));
        Assert.Equal([folder], handed);
    }

    [Fact]
    public void DropResolve_ViewerOnly_NeedsExistingFile_AndAudioAllowance()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        string audio = TSourceFileCreate(".mp3");
        try
        {
            Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TViewerDropResolve(source, [audio], true));
            TInterface.TViewerAllowSet(viewer, true);
            Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TViewerDropResolve(source, [audio], true));
            Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TViewerDropResolve(source, [audio], false));
        }
        finally
        {
            File.Delete(audio);
        }

        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TViewerDropResolve(source, [audio], true));
    }

    [Fact]
    public void DropHandle_ViewerOnly_OpensFirstExistingFile()
    {
        (LViewer viewer, LViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterface.TViewerMediaAttach(viewer, cargos.Add);
        string file = TSourceFileCreate(".txt");
        try
        {
            Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TViewerDropHandle(source, [file], true));
        }
        finally
        {
            File.Delete(file);
        }

        Assert.Single(cargos);
        Assert.Equal(file, cargos[0].LCargoSourcePath);
        Assert.NotNull(cargos[0].LCargoFfmpegError);
    }
}
