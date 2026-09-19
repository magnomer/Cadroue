using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LProgram
{
    private string? lDepotRootApplied;

    public static LScheduleContract LScheduleCurrent { get; } = new LSchedule();

    public void LProgramStart(
        string[] lArgs,
        Action<Action> lDispatch,
        Action<Action> lDefer,
        Func<IEnumerable<string>> lResourceNames,
        Func<string, string?> lResourceRead,
        IReadOnlyList<string> lPlacementKeys,
        Action lDebounceRestart,
        Action lFlyleafStart,
        Action<bool> lFlyleafVerbose)
    {
        LRelayChannel.LRelayStartupRead(lArgs);
        LRenderer.LRendererSettingsLoad();

        LLocalization.LLocalizationNamesSeam = lResourceNames;
        LLocalization.LLocalizationTextSeam = lResourceRead;
        LTraceLog.LTraceSeamAttach();
        LPreferenceStateStore.LPreferenceSeamAttach();
        LPreference.LPreferenceDebounceSeam = lDebounceRestart;
        LPreference.LPreferenceLoad();
        LFrameStore.LFrameLoad();
        LBinding.LBindingLoad();
        LScene.LSceneCurrentLoad();
        LLocalization.LLocalizationLoad(LPreference.LPreferenceStateCurrent.LPreferenceLanguage);
        LProgramLanguageNormalize();

        LPreference.LPreferenceDepotCallback = LProgramDepotHandle;
        LDepot.LDepotRootSet(LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LTrace.LTraceVerbose = LPreference.LPreferenceStateCurrent.LPreferenceLogVerbose;
        LFlyleaf.LFlyleafResolverAttach();
        LPlacement.LPlacementImport(
            LPreferenceStateStore.LPreferencePathRead(),
            lPlacementKeys.Select(lPlacementKey => (lPlacementKey, lPlacementKey)).ToArray());

        LRenderer.LRendererToolAttach();
        LSidecarStore.LSidecarLibrarianAttach();
        LSidecarStore.LSidecarSegmentAttach();
        LFlawScan.LFlawCheckupAttach();
        LPresetStore.LPresetSeamAttach();
        LPreset.LPresetPrepare();
        LProgramStationAttach(lDispatch);
        LAutopsy.LAutopsyProseAttach();
        LInventory.LInventoryPrepareStart();
        LTrialSet.LTrialSetStart();
        LRunner.LRunnerTraceAttach();
        LTraceLog.LTraceLoadingRecord(
            $"Application started: version {LTraceLog.LTraceVersionRead()}, process {Environment.ProcessId}");
        LTraceLog.LTraceLoadingRecord(LFlyleaf.LFlyleafActive
            ? "Local Flyleaf preview engine active"
            : "NuGet Flyleaf preview engine active");
        _ = LProgramDepotApply();
        LSectionPalette.LSectionRegistryLoad();
        LRetentionSweep.LRetentionSweepStart(
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupActive,
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupDays);
        LProgramStaleClaim();
        LRenderer.LRendererFlyleafSeam = lFlyleafStart;
        LRenderer.LRendererVerboseSeam = lFlyleafVerbose;
        LRenderer.LRendererDispatchSeam = lDefer;
        LRenderer.LRendererEngineStart();
        LRelayStore.LRelayStaleClear();
        LRelayChannel.LRelayChannelStart();
    }

    public void LProgramClose(int lExitCode)
    {
        LTraceLog.LTraceInfoRecord($"Application exiting with code {lExitCode}");
        LPreference.LPreferenceSaveCommit();
        LRelayChannel.LRelayChannelStop();
        LTraceWriter.LTraceWriterPersist();
    }

    public void LProgramDebounceTick() => LPreference.LPreferenceSaveCommit();

    private static void LProgramStationAttach(Action<Action> lDispatch)
    {
        LStation.LStationSchedule = LScheduleCurrent;
        LStation.LStationPost = lDispatch;
        LStation.LStationProgramSource = () => LRenderer.LRendererProgramCurrent;
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        LMessenger.LMessengerScheduleSource = () => LScheduleCurrent;
        LCartographer.LCartographerScheduleContract = LScheduleCurrent;
        LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                LCartographer.LCartographerAccept(
                    lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);
    }

    public static bool LProgramLanguageNormalize()
    {
        string lLocalizationLanguage = LLocalization.LLocalizationLanguageRead();
        if (string.Equals(
                lLocalizationLanguage,
                LPreference.LPreferenceStateCurrent.LPreferenceLanguage,
                StringComparison.Ordinal))
        {
            return false;
        }

        LPreferenceState lLocalizationPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lLocalizationPreference.LPreferenceLanguage = lLocalizationLanguage;
        LPreference.LPreferenceStateSet(lLocalizationPreference);
        return true;
    }

    private bool LProgramDepotHandle()
    {
        bool lDepotApplied = LProgramDepotApply();
        LProgramSidecarApply();
        return lDepotApplied;
    }

    public static void LProgramSidecarApply() =>
        LSidecarStore.LSidecarFolderApply(LPreference.LPreferenceStateCurrent.LPreferenceRecordWorkspace);

    public string? LProgramDepotRoot => lDepotRootApplied;

    public bool LProgramDepotApply()
    {
        try
        {
            string lDepotRoot = LDepot.LDepotRootResolve(
                LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
            if (string.Equals(lDepotRoot, lDepotRootApplied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lDepotRootApplied is string lDepotPrevious
                && !LDepot.LDepotFolderMove(lDepotPrevious, lDepotRoot))
            {
                return false;
            }

            if (lDepotRootApplied is null)
            {
                LDepot.LDepotRootSet(lDepotRoot);
            }

            LDepotIndex.LDepotIndexCreate();
            lDepotRootApplied = lDepotRoot;
            LProgramSidecarApply();
            LTraceLog.LTraceLoadingRecord($"Workspace at {lDepotRoot}");
            return true;
        }
        catch (Exception lException)
        {
            lDepotRootApplied = null;
            LTraceLog.LTraceErrorRecord("Workspace folder could not be prepared", lException);
            return false;
        }
    }

    private static void LProgramStaleClaim()
    {
        try
        {
            int lScheduleRecovered = LScheduleCurrent.LScheduleStaleClaim();
            if (lScheduleRecovered > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Worklist recovery: {lScheduleRecovered} interrupted job(s) resolved at startup");
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Worklist recovery failed at startup", lException);
        }
    }
}
