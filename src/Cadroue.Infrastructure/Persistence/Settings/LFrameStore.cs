using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LFrameStore
{
    private const string LFrameFolderName = "Cadroue";
    private const string LFrameFileName = "frame.json";

    private static bool lFrameReadable = true;

    public static LFrameState LFrameStateCurrent { get; private set; } = LFrameState.LFrameDefaultCreate();

    public static LFrameState LFrameLoad()
    {
        LVaultResult<LFrameState> lFrameResult = LVault.LVaultRead<LFrameState>(LFramePathCreate());
        lFrameReadable = lFrameResult.LVaultOutcome != LVaultOutcome.LVaultUnreadable;
        LFrameStateCurrent = lFrameResult.LVaultValue ?? LFrameState.LFrameDefaultCreate();
        LFrameStateCurrent.LFrameNormalize();
        return LFrameStateCurrent;
    }

    public static bool LFrameSave(LFrameState lFrameState)
    {
        LFrameStateCurrent = lFrameState;
        if (!lFrameReadable)
        {
            return false;
        }

        lFrameState.LFrameNormalize();
        return LVault.LVaultSave(LFramePathCreate(), lFrameState);
    }

    private static string LFramePathCreate()
    {
        string lFrameApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lFrameApplicationDataFolder, LFrameFolderName, LFrameFileName);
    }
}
