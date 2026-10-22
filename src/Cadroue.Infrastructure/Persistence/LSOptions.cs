using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSOptions
{
    public static LOptionsFact LSOptionsRead(LOptionsRequest request) => request.LOptionsRequestKind switch
    {
        LOptionsRequestKind.LOptionsRequestPalette =>
            new LOptionsFact(LDepot.LDepotPaletteRead(), 0, 0, false),
        LOptionsRequestKind.LOptionsRequestRoot =>
            new LOptionsFact(LDepot.LDepotRootRead(), 0, 0, false),
        LOptionsRequestKind.LOptionsRequestResolution =>
            new LOptionsFact(LDepot.LDepotRootResolve(request.LOptionsRequestPath), 0, 0, false),
        LOptionsRequestKind.LOptionsRequestDefault =>
            new LOptionsFact(LDepot.LDepotDefaultRead(), 0, 0, false),
        LOptionsRequestKind.LOptionsRequestOccupancy =>
            new LOptionsFact(string.Empty, 0, 0, LDepotRelocation.LDepotOccupiedCheck(
                request.LOptionsRequestPath ?? string.Empty)),
        LOptionsRequestKind.LOptionsRequestRunning =>
            new LOptionsFact(string.Empty, 0, 0, LDepot.LDepotRunningCheck(
                request.LOptionsRequestPath ?? string.Empty)),
        LOptionsRequestKind.LOptionsRequestSize =>
            new LOptionsFact(string.Empty, 0, LDepotUpkeep.LDepotSizeRead(), false),
        LOptionsRequestKind.LOptionsRequestSidecar =>
            new LOptionsFact(LSidecarPath.LSidecarFolderRead(), 0, 0, false),
        LOptionsRequestKind.LOptionsRequestLocation =>
            new LOptionsFact(LSidecarPath.LSidecarFolderResolve(), 0, 0, false),
        _ => throw new InvalidOperationException()
    };

    public static LOptionsFact LSOptionsApply(LOptionsRequest request) => request.LOptionsRequestKind switch
    {
        LOptionsRequestKind.LOptionsRequestDone => LSOptionsDoneClear(),
        LOptionsRequestKind.LOptionsRequestWorkspace => LSOptionsWorkspaceClear(),
        LOptionsRequestKind.LOptionsRequestSidecar =>
            new LOptionsFact(string.Empty, LSidecarPath.LSidecarFolderClear(), 0, false),
        LOptionsRequestKind.LOptionsRequestRenderer => LSOptionsApply(request.LOptionsRequestEngine),
        _ => throw new InvalidOperationException()
    };

    private static LOptionsFact LSOptionsDoneClear()
    {
        int count = LDepotUpkeep.LDepotFolderClear(
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed,
            LDepotFolder.LDepotFolderCancelled);
        _ = LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        return new LOptionsFact(string.Empty, count, 0, false);
    }

    private static LOptionsFact LSOptionsWorkspaceClear()
    {
        LDepotUpkeep.LDepotWorkspaceReset();
        _ = LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        return new LOptionsFact(string.Empty, 0, 0, false);
    }

    private static LOptionsFact LSOptionsApply(LPreviewEngine? engine)
    {
        LRenderer.LRendererEngineSet(engine ?? LPreviewEngine.LPreviewEngineFlyleaf);
        return new LOptionsFact(string.Empty, 0, 0, false);
    }
}
