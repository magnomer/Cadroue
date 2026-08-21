using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPreferenceStateStore
{
    private const string LPreferenceFolderName = "Cadroue";
    private const string LPreferenceFileName = "LPreferenceState.json";

    private static LVaultOutcome lPreferenceOutcome = LVaultOutcome.LVaultMissing;

    public static LPreferenceState LPreferenceStateLoad()
    {
        LVaultResult<LPreferenceState> lPreferenceResult = LVault.LVaultRead<LPreferenceState>(LPreferencePathCreate());
        lPreferenceOutcome = lPreferenceResult.LVaultOutcome;
        LPreferenceState lPreferenceState = lPreferenceResult.LVaultValue ?? LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceNormalize();
        return lPreferenceState;
    }

    public static bool LPreferenceStateSave(LPreferenceState lPreferenceState)
    {
        if (lPreferenceOutcome == LVaultOutcome.LVaultUnreadable)
        {
            return false;
        }

        lPreferenceState.LPreferenceNormalize();
        return LVault.LVaultSave(LPreferencePathCreate(), lPreferenceState);
    }

    public static string LPreferencePathRead() => LPreferencePathCreate();

    private static string LPreferencePathCreate()
    {
        string lPreferenceApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lPreferenceApplicationDataFolder, LPreferenceFolderName, LPreferenceFileName);
    }
}
