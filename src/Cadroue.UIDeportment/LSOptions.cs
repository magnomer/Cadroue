using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LSOptionsPage
{
    LSOptionsPageGeneral,
    LSOptionsPageSystem,
    LSOptionsPagePlayback,
    LSOptionsPageTimeline,
    LSOptionsPageWork
}

public sealed class LSOptions
{
    private readonly LPreferenceState lsOptionsDraft;
    private LSOptionsPage lsOptionsPage;
    private bool lsOptionsMpvEnabled;

    public event Action<bool>? LSOptionsMpvChange;

    public LSOptions()
        : this(LPreference.LPreferenceStateCurrent.LPreferenceClone(), LMpv.LMpvAvailableCheck())
    {
    }

    public LSOptions(LPreferenceState lDraft, bool lMpvEnabled)
    {
        lsOptionsDraft = lDraft;
        lsOptionsMpvEnabled = lMpvEnabled;
    }

    public LPreferenceState LSOptionsDraft => lsOptionsDraft;

    public LSOptionsPage LSOptionsPage => lsOptionsPage;

    public bool LSOptionsMpvEnabled => lsOptionsMpvEnabled;

    public void LSOptionsPageSelect(LSOptionsPage lPage) => lsOptionsPage = lPage;

    public string LSOptionsEngineRead() =>
        lsOptionsMpvEnabled && string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
            ? "Mpv"
            : "Flyleaf";

    public void LSOptionsMpvSet(bool lEnabled)
    {
        lsOptionsMpvEnabled = lEnabled;
        LSOptionsMpvChange?.Invoke(lEnabled);
    }

    public async Task LSOptionsMpvUpdate() => LSOptionsMpvSet(await LRenderer.LRendererMpvCheck());

    public static string LSOptionsMpvResolve() =>
        LMpv.LMpvInstalledCheck() ? "Options.System.ReinstallMpv" : "Options.System.DownloadMpv";

    public static string LSOptionsFlyleafResolve() =>
        LFlyleaf.LFlyleafInstalledCheck() ? "Options.System.ReinstallFlyleaf" : "Options.System.InstallFlyleaf";

    public static string LSOptionsFfmpegResolve(string lFolder)
    {
        if (string.IsNullOrWhiteSpace(lFolder))
        {
            return "Options.System.FFmpegBlank";
        }

        bool lProgramReady = LRendererLibrary.LRendererProgramExist(lFolder);
        bool lLibraryReady = LRendererLibrary.LRendererFolderValidate(lFolder);
        if (lProgramReady && lLibraryReady)
        {
            return "Options.System.FFmpegReady";
        }

        if (lProgramReady)
        {
            return "Options.System.FFmpegProgramOnly";
        }

        return lLibraryReady ? "Options.System.FFmpegLibraryOnly" : "Options.System.FFmpegMissing";
    }

    public static bool LSOptionsAppliedCheck(string lFolder) =>
        string.Equals(LDepot.LDepotRootResolve(lFolder), LDepot.LDepotRootRead(), StringComparison.OrdinalIgnoreCase);

    public static string LSOptionsWorkspaceResolve(string lFolder, out bool lOccupied)
    {
        string lCurrent = LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder;
        string lNext = LDepot.LDepotRootResolve(lFolder);
        lOccupied = !string.Equals(lNext, LDepot.LDepotRootResolve(lCurrent), StringComparison.OrdinalIgnoreCase)
            && LDepot.LDepotOccupiedCheck(lNext);
        return lOccupied ? lCurrent : lFolder;
    }

    public bool LSOptionsRecordCheck() => lsOptionsDraft.LPreferenceRecordWorkspace;

    public bool LSOptionsApply()
    {
        bool lSaved = LPreference.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        LRenderer.LRendererEngineSet(
            string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        return lSaved;
    }

    public bool LSOptionsRestartCheck() =>
        !string.Equals(
            LLocalization.LLocalizationLanguageNormalize(LPreference.LPreferenceStateCurrent.LPreferenceLanguage),
            LLocalization.LLocalizationLanguageRead(),
            StringComparison.OrdinalIgnoreCase);
}
