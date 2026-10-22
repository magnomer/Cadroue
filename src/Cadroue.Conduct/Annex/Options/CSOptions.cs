using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed class CSOptions : LOptionsPort
{
    private readonly (Func<LOptionsRequest, LOptionsFact>, Func<LOptionsRequest, LOptionsFact>) csOptionsPort;
    private readonly CUsher csOptionsUsher;
    private readonly LPreferenceState csOptionsDraft;

    public CSOptions(
        CUsher usher,
        LPreferenceState draft,
        Func<LOptionsRequest, LOptionsFact> read,
        Func<LOptionsRequest, LOptionsFact> apply)
    {
        csOptionsUsher = usher;
        csOptionsDraft = draft;
        csOptionsPort = (read, apply);
        CSOptionsUpdate();
    }

    public LOptionsSnapshot CSOptionsSnapshot { get; private set; } = null!;

    public LOptionsSnapshot CSOptionsSnapshotRead()
    {
        CSOptionsUpdate();
        return CSOptionsSnapshot with
        {
            CSOptionsSelection = Array.AsReadOnly(CSOptionsSnapshot.CSOptionsSelection.ToArray())
        };
    }

    public LOptionsState CSOptionsDraftRead<LOptionsState>() where LOptionsState : class =>
        (LOptionsState)(object)csOptionsDraft;

    public LOptionsApplyResult CSOptionsApply()
    {
        CSOptionsUpdate();
        CSOptionsUpdate(CSOptionsSnapshot);
        bool saved = LPreference.LPreferenceStateSet(csOptionsDraft.LPreferenceClone());
        _ = CSOptionsRequestApply(
            LOptionsRequestKind.LOptionsRequestRenderer,
            engine: string.Equals(CSOptionsSnapshot.CSOptionsEngine, "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        return new LOptionsApplyResult(saved);
    }

    public string CSOptionsWorkspaceResolve(string folder, Action<string, string> occupied)
    {
        LOptionsWorkspaceResult result = CSOptionsWorkspaceResolve(folder, csOptionsPort.Item1);
        if (result.CSOptionsWorkspaceOccupied)
        {
            occupied(result.CSOptionsWorkspaceKept, result.CSOptionsWorkspaceDestination);
        }
        return result.CSOptionsWorkspaceKept;
    }

    public void CSOptionsWorkspaceCommit(string folder, Action<string, string> occupied) =>
        csOptionsDraft.LPreferenceWorkspaceFolder = CSOptionsWorkspaceResolve(folder, occupied);

    public bool CSOptionsRestartCheck() =>
        !string.Equals(
            LLocalization.LLocalizationLanguageNormalize(LPreference.LPreferenceStateCurrent.LPreferenceLanguage),
            LLocalization.LLocalizationLanguageRead(),
            StringComparison.OrdinalIgnoreCase);

    public bool CSOptionsFolderExist(string folder) => csOptionsUsher.CUsherFolderExist(folder);

    public string? CSOptionsFolderOpen(string folder, string fallback) =>
        csOptionsUsher.CUsherFolderOpen(string.IsNullOrWhiteSpace(folder) ? fallback : folder);

    public string? CSOptionsFolderOpen(string folder) =>
        csOptionsUsher.CUsherFolderOpen(CSOptionsDefaultRead(folder));

    public string CSOptionsEngineRead(bool enabled) =>
        enabled && string.Equals(CSOptionsSnapshotRead().CSOptionsEngine, "Mpv", StringComparison.Ordinal)
            ? "Mpv"
            : "Flyleaf";

    Func<bool, string> LOptionsPort.CSOptionsEngineRead() => enabled => CSOptionsEngineRead(enabled);

    public void CSOptionsAppliedCheck(string folder, Action<string> applied, Action unapplied)
    {
        LOptionsFact result = CSOptionsAppliedCheck(folder, csOptionsPort.Item1);
        if (result.LOptionsFactState)
        {
            applied(result.LOptionsFactPath);
            return;
        }
        unapplied();
    }

    public string CSOptionsPaletteRead() =>
        CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestPalette).LOptionsFactPath;

    public void CSOptionsLevelSet(LCustodyFamily family, int level)
    {
        csOptionsDraft.LPreferenceLevelSet(family, level);
        CSOptionsUpdate();
    }

    public bool CSOptionsRecordCheck()
    {
        CSOptionsUpdate();
        return CSOptionsSnapshot.CSOptionsRecordMode;
    }

    public string CSOptionsRecordResolve() =>
        CSOptionsRecordCheck() ? "Options.System.FileRecordWorkspace" : "Options.System.FileRecordBeside";

    public string CSOptionsSidecarRead() =>
        CSOptionsSidecarRead(csOptionsPort.Item1);

    public LOptionsFact CSOptionsDoneClear()
    {
        LOptionsFact result = CSOptionsRequestApply(LOptionsRequestKind.LOptionsRequestDone);
        return result with
        {
            LOptionsFactSize = CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestSize).LOptionsFactSize
        };
    }

    public LOptionsFact CSOptionsWorkspaceClear()
    {
        LOptionsFact result = CSOptionsRequestApply(LOptionsRequestKind.LOptionsRequestWorkspace);
        return result with
        {
            LOptionsFactSize = CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestSize).LOptionsFactSize
        };
    }

    public string CSOptionsDefaultRead(string folder) => string.IsNullOrWhiteSpace(folder)
        ? CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestDefault).LOptionsFactPath
        : folder;

    public void CSOptionsDefaultCheck(string folder, Action defaulted, Action custom)
    {
        if (string.Equals(CSOptionsDefaultRead(folder), folder, StringComparison.Ordinal))
        {
            custom();
            return;
        }
        defaulted();
    }

    public long CSOptionsSizeRead() => CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestSize).LOptionsFactSize;

    public void CSOptionsRunningCheck(string folder, Action<string> stopped, Action running)
    {
        LOptionsFact applied = CSOptionsAppliedCheck(folder, csOptionsPort.Item1);
        if (!applied.LOptionsFactState)
        {
            return;
        }
        string root = applied.LOptionsFactPath;
        if (CSOptionsRequestRead(LOptionsRequestKind.LOptionsRequestRunning, root).LOptionsFactState)
        {
            running();
            return;
        }
        stopped(root);
    }

    public void CSOptionsUpdate(
        IReadOnlyList<string> selection,
        bool recent,
        bool safety,
        bool vacancy,
        string language)
    {
        CSOptionsSnapshot = CSOptionsSnapshot with
        {
            CSOptionsSelection = Array.AsReadOnly(selection.ToArray()),
            CSOptionsRecent = recent,
            CSOptionsSafety = safety,
            CSOptionsVacancy = vacancy,
            CSOptionsLanguage = language
        };
    }

    public void CSOptionsUpdate(string startup) =>
        CSOptionsSnapshot = CSOptionsSnapshot with { CSOptionsStartup = startup };

    public void CSOptionsUpdate(bool orientation) =>
        CSOptionsSnapshot = CSOptionsSnapshot with { CSOptionsOrientation = orientation };

    public static LOptionsWorkspaceResult CSOptionsWorkspaceResolve(
        string folder,
        Func<LOptionsRequest, LOptionsFact> read)
    {
        string current = LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder;
        string destination = read(new LOptionsRequest(
            LOptionsRequestKind.LOptionsRequestResolution, folder)).LOptionsFactPath;
        string origin = read(new LOptionsRequest(
            LOptionsRequestKind.LOptionsRequestResolution, current)).LOptionsFactPath;
        bool occupied = !string.Equals(destination, origin, StringComparison.OrdinalIgnoreCase)
            && read(new LOptionsRequest(
                LOptionsRequestKind.LOptionsRequestOccupancy, destination)).LOptionsFactState;
        return new LOptionsWorkspaceResult(occupied ? current : folder, destination, occupied);
    }

    public static LOptionsFact CSOptionsAppliedCheck(string folder, Func<LOptionsRequest, LOptionsFact> read)
    {
        string destination = read(new LOptionsRequest(
            LOptionsRequestKind.LOptionsRequestResolution,
            folder)).LOptionsFactPath;
        string root = read(new LOptionsRequest(LOptionsRequestKind.LOptionsRequestRoot)).LOptionsFactPath;
        return new LOptionsFact(
            root,
            0,
            0,
            string.Equals(destination, root, StringComparison.OrdinalIgnoreCase));
    }

    public static string CSOptionsSidecarRead(Func<LOptionsRequest, LOptionsFact> read) =>
        read(new LOptionsRequest(LOptionsRequestKind.LOptionsRequestLocation)).LOptionsFactPath;

    private LOptionsFact CSOptionsRequestRead(
        LOptionsRequestKind kind,
        string? path = null) =>
        csOptionsPort.Item1(new LOptionsRequest(kind, path));

    private LOptionsFact CSOptionsRequestApply(
        LOptionsRequestKind kind,
        string? path = null,
        LPreviewEngine? engine = null) =>
        csOptionsPort.Item2(new LOptionsRequest(kind, path, engine));

    private void CSOptionsUpdate()
    {
        CSOptionsSnapshot = new LOptionsSnapshot(
            CSOptionsSnapshot is null ? csOptionsDraft.LPreferenceStartupMode : CSOptionsSnapshot.CSOptionsStartup,
            Array.AsReadOnly((CSOptionsSnapshot is null
                ? csOptionsDraft.LPreferenceStartupTabs
                : CSOptionsSnapshot.CSOptionsSelection).ToArray()),
            CSOptionsSnapshot is null ? csOptionsDraft.LPreferenceMediaAutomatic : CSOptionsSnapshot.CSOptionsRecent,
            CSOptionsSnapshot is null
                ? csOptionsDraft.LPreferenceConfirmDestructive
                : CSOptionsSnapshot.CSOptionsSafety,
            CSOptionsSnapshot is null ? csOptionsDraft.LPreferenceRelayEmpty : CSOptionsSnapshot.CSOptionsVacancy,
            CSOptionsSnapshot is null ? csOptionsDraft.LPreferenceVerticalTabs : CSOptionsSnapshot.CSOptionsOrientation,
            CSOptionsSnapshot is null ? csOptionsDraft.LPreferenceLanguage : CSOptionsSnapshot.CSOptionsLanguage,
            csOptionsDraft.LPreferenceAutoplay,
            csOptionsDraft.LPreferenceVolumeMode,
            csOptionsDraft.LPreferenceVolume,
            csOptionsDraft.LPreferenceWheelAction,
            csOptionsDraft.LPreferenceDragPaused,
            csOptionsDraft.LPreferencePreviewEngine,
            csOptionsDraft.LPreferenceTimelineOrder,
            csOptionsDraft.LPreferenceKeyframePixels,
            csOptionsDraft.LPreferenceKeyframeDelay,
            csOptionsDraft.LPreferenceSectionPalette,
            csOptionsDraft.LPreferenceOverlapAllowed,
            csOptionsDraft.LPreferenceWaveform,
            csOptionsDraft.LPreferenceFailurePaused,
            csOptionsDraft.LPreferenceRetryAllowed,
            csOptionsDraft.LPreferenceRetryMaximum,
            csOptionsDraft.LPreferenceRecordWorkspace,
            csOptionsDraft.LPreferenceCleanupActive,
            csOptionsDraft.LPreferenceCleanupDays,
            csOptionsDraft.LPreferenceWorkspaceFolder,
            csOptionsDraft.LPreferenceFfmpegFolder,
            csOptionsDraft.LPreferenceLevelRead(LCustodyFamily.LCustodyFamilyEncode),
            csOptionsDraft.LPreferenceLevelRead(LCustodyFamily.LCustodyFamilyKeyframe),
            csOptionsDraft.LPreferenceLevelRead(LCustodyFamily.LCustodyFamilyWaveform));
    }

    private void CSOptionsUpdate(LOptionsSnapshot snapshot)
    {
        csOptionsDraft.LPreferenceStartupMode = snapshot.CSOptionsStartup;
        csOptionsDraft.LPreferenceStartupTabs = snapshot.CSOptionsSelection.ToList();
        csOptionsDraft.LPreferenceMediaAutomatic = snapshot.CSOptionsRecent;
        csOptionsDraft.LPreferenceConfirmDestructive = snapshot.CSOptionsSafety;
        csOptionsDraft.LPreferenceRelayEmpty = snapshot.CSOptionsVacancy;
        csOptionsDraft.LPreferenceVerticalTabs = snapshot.CSOptionsOrientation;
        csOptionsDraft.LPreferenceLanguage = snapshot.CSOptionsLanguage;
        csOptionsDraft.LPreferenceAutoplay = snapshot.CSOptionsAutoplay;
        csOptionsDraft.LPreferenceVolumeMode = snapshot.CSOptionsVolumeMode;
        csOptionsDraft.LPreferenceVolume = snapshot.CSOptionsVolume;
        csOptionsDraft.LPreferenceWheelAction = snapshot.CSOptionsWheel;
        csOptionsDraft.LPreferenceDragPaused = snapshot.CSOptionsDrag;
        csOptionsDraft.LPreferencePreviewEngine = snapshot.CSOptionsEngine;
        csOptionsDraft.LPreferenceTimelineOrder = snapshot.CSOptionsOrder;
        csOptionsDraft.LPreferenceKeyframePixels = snapshot.CSOptionsKeyframe;
        csOptionsDraft.LPreferenceKeyframeDelay = snapshot.CSOptionsDelay;
        csOptionsDraft.LPreferenceSectionPalette = snapshot.CSOptionsPalette;
        csOptionsDraft.LPreferenceOverlapAllowed = snapshot.CSOptionsOverlap;
        csOptionsDraft.LPreferenceWaveform = snapshot.CSOptionsWaveform;
        csOptionsDraft.LPreferenceFailurePaused = snapshot.CSOptionsFailure;
        csOptionsDraft.LPreferenceRetryAllowed = snapshot.CSOptionsRetry;
        csOptionsDraft.LPreferenceRetryMaximum = snapshot.CSOptionsRetryMaximum;
        csOptionsDraft.LPreferenceRecordWorkspace = snapshot.CSOptionsRecordMode;
        csOptionsDraft.LPreferenceCleanupActive = snapshot.CSOptionsCleanup;
        csOptionsDraft.LPreferenceCleanupDays = snapshot.CSOptionsCleanupDays;
        csOptionsDraft.LPreferenceWorkspaceFolder = snapshot.CSOptionsWorkspace;
        csOptionsDraft.LPreferenceFfmpegFolder = snapshot.CSOptionsRenderer;
        csOptionsDraft.LPreferenceLevelSet(LCustodyFamily.LCustodyFamilyEncode, snapshot.CSOptionsEncodeLevel);
        csOptionsDraft.LPreferenceLevelSet(LCustodyFamily.LCustodyFamilyKeyframe, snapshot.CSOptionsKeyframeLevel);
        csOptionsDraft.LPreferenceLevelSet(LCustodyFamily.LCustodyFamilyWaveform, snapshot.CSOptionsWaveformLevel);
    }
}
