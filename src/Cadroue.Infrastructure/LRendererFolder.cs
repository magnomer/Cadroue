using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRendererFolder
{
    public static Func<string> LRendererFolderSeam { get; set; } = () => string.Empty;

    public static LRendererSettings LRendererSettingsCurrent { get; private set; } =
        LRendererSettings.LRendererDefaultCreate();

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LRendererFolderSeam())
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LRendererFolderSeam();

    public static string LRendererProgramCurrent =>
        LRendererLibrary.LRendererProgramRead(LRendererFolderCurrent);

    public static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

    public static void LRendererToolAttach() => LTool.LToolFolderSource = () => LRendererFolderCurrent;

    public static string LRendererFfmpegResolve(string lRendererDefaultPath)
    {
        if (LRendererLibrary.LRendererFolderValidate(LRendererFolderSeam()))
        {
            return LRendererFolderSeam();
        }

        if (LRendererLibrary.LRendererFolderValidate(LRendererSettingsCurrent.LRendererLibraryFolder))
        {
            return LRendererSettingsCurrent.LRendererLibraryFolder!;
        }

        return LRendererLibrary.LRendererFolderFind() ?? lRendererDefaultPath;
    }
}
