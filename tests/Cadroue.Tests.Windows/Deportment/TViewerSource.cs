using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerSource
{
    private static (QViewer TViewer, QViewerSource TSource) TSourceBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        return (viewer, TInterfaceViewer.TViewerSourceRead(viewer));
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
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        TInterfaceViewer.TViewerCommandSet(viewer, false);

        TInterfaceViewer.TViewerSourceOpen(source, "clip.mp4");

        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, "clip.mp4"));
    }

    [Fact]
    public void SourceOpen_PlainPath_StartsLoad_AndReportsMissingFile()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-open-{Guid.NewGuid():N}.mp4");

        TInterfaceViewer.TViewerSourceOpen(source, path);

        Assert.Single(cargos);
        Assert.Equal(path, cargos[0].LCargoSourcePath);
        Assert.NotNull(cargos[0].LCargoFfmpegError);
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
    }

    [Fact]
    public void SourceOpen_SidecarUnresolved_AsksWarning_AndRefuses()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        List<QViewerAskKind> asks = [];
        TInterfaceViewer.TViewerAskAttach(source, (ask, answer) =>
        {
            asks.Add(ask.QViewerAskKind);
            answer(true);
        });
        TInterfaceViewer.TViewerLibrarianAttach(
            path => path.EndsWith(".cad", StringComparison.Ordinal), _ => null, null);
        try
        {
            TInterfaceViewer.TViewerSourceOpen(source, "clip.cad");
        }
        finally
        {
            TInterfaceViewer.TViewerLibrarianAttach(null, null, null);
        }

        Assert.Equal([QViewerAskKind.QViewerAskWarning], asks);
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
    }

    [Fact]
    public void SourceOpen_SidecarMismatch_DecisionAccepted_OpensResolvedPath()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        List<QViewerAskKind> asks = [];
        TInterfaceViewer.TViewerAskAttach(source, (ask, answer) =>
        {
            asks.Add(ask.QViewerAskKind);
            answer(true);
        });
        string resolved = Path.Combine(Path.GetTempPath(), $"cadroue-resolved-{Guid.NewGuid():N}.mp4");
        TInterfaceViewer.TViewerLibrarianAttach(
            path => path.EndsWith(".cad", StringComparison.Ordinal),
            _ => TInterfaceViewer.TSidecarSourceCreate(resolved, LSidecarSourceKind.LSidecarSourceSibling),
            null);
        try
        {
            TInterfaceViewer.TViewerSourceOpen(source, "clip.cad");
        }
        finally
        {
            TInterfaceViewer.TViewerLibrarianAttach(null, null, null);
        }

        Assert.Equal([QViewerAskKind.QViewerAskDecision], asks);
        Assert.Single(cargos);
        Assert.Equal(resolved, cargos[0].LCargoSourcePath);
    }

    [Fact]
    public void PathHandle_SameSource_DoesNotReopen_OtherSourceDoes()
    {
        (QViewer viewer, _) = TSourceBuild();
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-same-{Guid.NewGuid():N}.mp4");
        TInterfaceViewer.TViewerRequestSet(viewer, path);
        TInterfaceViewer.TViewerIntentSet(viewer, path, TimeSpan.Zero, null);
        int serial = viewer.QViewerTrace.CViewerSerial;

        TInterfaceViewer.TViewerPathHandle(viewer, path);
        Assert.Equal(serial, viewer.QViewerTrace.CViewerSerial);

        TInterfaceViewer.TViewerPathHandle(viewer, Path.Combine(Path.GetTempPath(), "cadroue-other.mp4"));
        Assert.NotEqual(serial, viewer.QViewerTrace.CViewerSerial);
    }

    [Fact]
    public void DropResolve_ListPresent_AcceptsFoldersAndMedia()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        TInterfaceViewer.TViewerDropAttach(source, _ => { });
        string folder = Path.GetTempPath();

        Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterfaceViewer.TViewerDropResolve(source, [folder], true));
        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterfaceViewer.TViewerDropResolve(source, [folder], false));
        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterfaceViewer.TViewerDropResolve(source, null, true));
        Assert.Equal(
            QWindowDropEffect.QWindowDropNone,
            TInterfaceViewer.TViewerDropResolve(
                source, [Path.Combine(folder, $"missing-{Guid.NewGuid():N}.txt")], true));
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
    }

    [Fact]
    public void DropHandle_ListPresent_HandsPathsToList()
    {
        (_, QViewerSource source) = TSourceBuild();
        List<string> handed = [];
        TInterfaceViewer.TViewerDropAttach(source, paths => handed.AddRange(paths));
        string folder = Path.GetTempPath();

        Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterfaceViewer.TViewerDropHandle(source, [folder], true));
        Assert.Equal([folder], handed);
    }

    [Fact]
    public void DropResolve_ViewerOnly_NeedsExistingFile_AndAudioAllowance()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        string audio = TSourceFileCreate(".mp3");
        try
        {
            Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterfaceViewer.TViewerDropResolve(source, [audio], true));
            TInterfaceViewer.TViewerAllowSet(viewer, true);
            Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterfaceViewer.TViewerDropResolve(source, [audio], true));
            Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterfaceViewer.TViewerDropResolve(
                source, [audio], false));
        }
        finally
        {
            File.Delete(audio);
        }

        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterfaceViewer.TViewerDropResolve(source, [audio], true));
    }

    [Fact]
    public void DropHandle_ViewerOnly_OpensFirstExistingFile()
    {
        (QViewer viewer, QViewerSource source) = TSourceBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        string file = TSourceFileCreate(".txt");
        try
        {
            Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterfaceViewer.TViewerDropHandle(source, [file], true));
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
