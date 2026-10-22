namespace Cadroue.Core
{
    public enum LOptionsRequestKind
    {
        LOptionsRequestPalette,
        LOptionsRequestRoot,
        LOptionsRequestResolution,
        LOptionsRequestDefault,
        LOptionsRequestOccupancy,
        LOptionsRequestRunning,
        LOptionsRequestSize,
        LOptionsRequestDone,
        LOptionsRequestWorkspace,
        LOptionsRequestSidecar,
        LOptionsRequestLocation,
        LOptionsRequestRenderer
    }

    public sealed record LOptionsRequest(
        LOptionsRequestKind LOptionsRequestKind,
        string? LOptionsRequestPath = null,
        LPreviewEngine? LOptionsRequestEngine = null);

    public sealed record LOptionsFact(
        string LOptionsFactPath,
        int LOptionsFactCount,
        long LOptionsFactSize,
        bool LOptionsFactState);
}

namespace Cadroue.Core
{
    public sealed record LOptionsSnapshot(
        string CSOptionsStartup,
        IReadOnlyList<string> CSOptionsSelection,
        bool CSOptionsRecent,
        bool CSOptionsSafety,
        bool CSOptionsVacancy,
        bool CSOptionsOrientation,
        string CSOptionsLanguage,
        bool CSOptionsAutoplay,
        string CSOptionsVolumeMode,
        double CSOptionsVolume,
        string CSOptionsWheel,
        bool CSOptionsDrag,
        string CSOptionsEngine,
        string CSOptionsOrder,
        double CSOptionsKeyframe,
        double CSOptionsDelay,
        string CSOptionsPalette,
        bool CSOptionsOverlap,
        bool CSOptionsWaveform,
        bool CSOptionsFailure,
        bool CSOptionsRetry,
        double CSOptionsRetryMaximum,
        bool CSOptionsRecordMode,
        bool CSOptionsCleanup,
        int CSOptionsCleanupDays,
        string CSOptionsWorkspace,
        string CSOptionsRenderer,
        int CSOptionsEncodeLevel,
        int CSOptionsKeyframeLevel,
        int CSOptionsWaveformLevel);

    public sealed record LOptionsApplyResult(bool CSOptionsSaved);

    public sealed record LOptionsWorkspaceResult(
        string CSOptionsWorkspaceKept,
        string CSOptionsWorkspaceDestination,
        bool CSOptionsWorkspaceOccupied);

    public interface LOptionsPort
    {
        LOptionsSnapshot CSOptionsSnapshotRead();

        LOptionsState CSOptionsDraftRead<LOptionsState>() where LOptionsState : class;

        LOptionsApplyResult CSOptionsApply();

        void CSOptionsUpdate(
            IReadOnlyList<string> selection,
            bool recent,
            bool safety,
            bool vacancy,
            string language);

        void CSOptionsUpdate(string startup);

        void CSOptionsUpdate(bool orientation);

        string CSOptionsWorkspaceResolve(string folder, Action<string, string> occupied);

        void CSOptionsWorkspaceCommit(string folder, Action<string, string> occupied);

        bool CSOptionsRestartCheck();

        bool CSOptionsFolderExist(string folder);

        string? CSOptionsFolderOpen(string folder, string fallback);

        string? CSOptionsFolderOpen(string folder);

        Func<bool, string> CSOptionsEngineRead();

        void CSOptionsAppliedCheck(string folder, Action<string> applied, Action unapplied);

        string CSOptionsPaletteRead();

        void CSOptionsLevelSet(Cadroue.Core.LCustodyFamily family, int level);

        bool CSOptionsRecordCheck();

        string CSOptionsRecordResolve();

        string CSOptionsSidecarRead();

        LOptionsFact CSOptionsDoneClear();

        LOptionsFact CSOptionsWorkspaceClear();

        string CSOptionsDefaultRead(string folder);

        void CSOptionsDefaultCheck(string folder, Action defaulted, Action custom);

        long CSOptionsSizeRead();

        void CSOptionsRunningCheck(string folder, Action<string> stopped, Action running);
    }
}
