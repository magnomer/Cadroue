using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QProgram
{
    private static Action<Action>? qProgramDeferSeam;
    private readonly Action<Action<Action>> qProgramStation;

    public QProgram(LScheduleContract lSchedule, Action<Action<Action>> lStation)
    {
        QScheduleCurrent = lSchedule;
        qProgramStation = lStation;
    }

    public static LScheduleContract QScheduleCurrent { get; private set; } = null!;

    public QProgramDepot QProgramDepot { get; } = new();

    public void QProgramStart(
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
        QPlacement.QPlacementRootSeam = LDepot.LDepotRootRead;
        LRelayStartup.LRelayStartupRead(lArgs);
        LRendererFolder.LRendererSettingsLoad();

        QProgramSeamAttach(lResourceNames, lResourceRead, lDebounceRestart);
        QProgramKit.QProgramKitApply();
        LPreference.LPreferenceLoad();
        LFrameStore.LFrameLoad();
        LBinding.LBindingLoad();
        LScene.LSceneCurrentLoad();
        QProgramLanguageLoad(LPreference.LPreferenceStateCurrent.LPreferenceLanguage);
        QProgramLanguageNormalize();

        LPreference.LPreferenceDepotCallback = QProgramDepot.QProgramDepotHandle;
        LDepot.LDepotRootSet(LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LTrace.LTraceVerbose = LPreference.LPreferenceStateCurrent.LPreferenceLogVerbose;
        LFlyleaf.LFlyleafResolverAttach();
        QPlacement.QPlacementImport(lPlacementKeys.Select(lPlacementKey => (lPlacementKey, lPlacementKey)).ToArray());

        LRendererFolder.LRendererToolAttach();
        LPresetPersistence.LPresetPrepare();
        qProgramStation(lDispatch);
        LAutopsy.LAutopsyProseAttach();
        LInventory.LInventoryPrepareStart();
        LTrialCatalog.LTrialSetStart();
        QProgramStartRecord();
        _ = QProgramDepot.QProgramDepotApply();
        LSectionPalette.LSectionRegistryLoad();
        LRetentionSweep.LRetentionSweepStart(
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupActive,
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupDays);
        QProgramStaleClaim();
        QProgramRendererAttach(lFlyleafStart, lFlyleafVerbose, lDefer);
        LRenderer.LRendererEngineStart();
        LRelayStore.LRelayStaleClear();
        LRelayChannel.LRelayChannelStart();
    }

    private static void QProgramSeamAttach(
        Func<IEnumerable<string>> lResourceNames,
        Func<string, string?> lResourceRead,
        Action lDebounceRestart)
    {
        LLocalizationResource.LLocalizationNamesSeam = lResourceNames;
        LLocalizationResource.LLocalizationTextSeam = lResourceRead;
        LPreference.LPreferenceDebounceSeam = lDebounceRestart;
    }

    private static void QProgramStartRecord()
    {
        LTraceLog.LTraceLoadingRecord(
            $"Application started: version {LProgramVersion.LProgramVersionRead()}, process {Environment.ProcessId}");
        LTraceLog.LTraceLoadingRecord(LFlyleaf.LFlyleafActive
            ? "Local Flyleaf preview engine active"
            : "NuGet Flyleaf preview engine active");
    }

    private static void QProgramRendererAttach(
        Action lFlyleafStart,
        Action<bool> lFlyleafVerbose,
        Action<Action> lDefer)
    {
        LRenderer.LRendererFlyleafSeam = lFlyleafStart;
        LRendererLog.LRendererVerboseSeam = lFlyleafVerbose;
        LRenderer.LRendererDispatchSeam = lDefer;
        qProgramDeferSeam = lDefer;
    }

    public void QProgramClose(int lExitCode)
    {
        LTraceLog.LTraceInfoRecord($"Application exiting with code {lExitCode}");
        LPreference.LPreferenceSaveCommit();
        LRelayChannel.LRelayChannelStop();
        LSidecarPending.LSidecarPersist();
        LTraceWriter.LTraceWriterPersist();
    }

    public static void QProgramDebounceTick() => LPreference.LPreferenceSaveCommit();

    public static void QProgramDefer(Action lAction)
    {
        if (qProgramDeferSeam is { } lDefer)
        {
            lDefer(lAction);
            return;
        }

        lAction();
    }

    public static void QProgramLanguageLoad(string? lLanguage)
    {
        LLocalization.LLocalizationLoad(lLanguage);
        QLocalizationCatalog.QLocalizationCatalogApply(
            System.Windows.Application.Current?.Resources,
            LLocalization.LLocalizationTextsRead(),
            LLocalization.LLocalizationFallbackRead(),
            LLocalization.LLocalizationTraceRecord);
    }

    public static bool QProgramLanguageNormalize()
    {
        string lLocalizationLanguage = LLocalization.LLocalizationLanguageRead();
        if (string.Equals(
                lLocalizationLanguage,
                LPreference.LPreferenceStateCurrent.LPreferenceLanguage,
                StringComparison.Ordinal))
        {
            return false;
        }

        QProgramLanguageSet(lLocalizationLanguage);
        return true;
    }

    private static void QProgramLanguageSet(string lLocalizationLanguage)
    {
        LPreferenceState lLocalizationPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lLocalizationPreference.LPreferenceLanguage = lLocalizationLanguage;
        LPreference.LPreferenceStateSet(lLocalizationPreference);
    }

    private static void QProgramStaleClaim()
    {
        try
        {
            int lScheduleRecovered = LScheduleStale.LScheduleStaleClaim();
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
