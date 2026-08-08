using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRenderer
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    public static string LRendererProgramCurrent =>
        LRendererLibrary.LRendererProgramRead(LRendererFolderCurrent);

    public static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();
}
