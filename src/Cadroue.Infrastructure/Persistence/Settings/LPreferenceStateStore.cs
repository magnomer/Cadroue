using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LPreferenceStateStore
{
    private const string LPreferenceFileName = "LPreferenceState.json";

    private static LVaultOutcome lPreferenceOutcome = LVaultOutcome.LVaultMissing;

    public static LPreferenceState? LPreferenceStateLoad()
    {
        LVaultResult<LPreferenceState> lPreferenceResult = LVault.LVaultRead<LPreferenceState>(LPreferencePathCreate());
        lPreferenceOutcome = lPreferenceResult.LVaultOutcome;
        return lPreferenceResult.LVaultValue;
    }

    public static bool LPreferenceStateSave(LPreferenceState lPreferenceState)
    {
        if (lPreferenceOutcome == LVaultOutcome.LVaultUnreadable)
        {
            return false;
        }

        return LVault.LVaultSave(LPreferencePathCreate(), lPreferenceState);
    }

    public static string LPreferencePathRead() => LPreferencePathCreate();

    private static string LPreferencePathCreate() => Path.Combine(LDepot.LDepotHomeRead(), LPreferenceFileName);
}
