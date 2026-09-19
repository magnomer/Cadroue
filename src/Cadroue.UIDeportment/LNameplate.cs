using Cadroue.Application;

namespace Cadroue.UIDeportment;

public static class LNameplate
{
    private const string LNameplateJoin = " › ";
    private const string LNameplateRoot = "Cadroue";

    public static bool LNameplateDeveloper => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public static bool LNameplateOwnedResolve(object? lTooltip, bool? lOwned)
    {
        bool lOwnedNow = lOwned ?? false;
        if (!LNameplateDeveloper || (lTooltip is not null && !lOwnedNow))
        {
            return lOwnedNow;
        }

        return true;
    }

    public static object? LNameplateTipResolve(bool? lOwned, object? lTooltip, Func<string> lResolve) =>
        LNameplateDeveloper && lOwned == true ? lResolve() : lTooltip;

    public static bool LNameplateTipCheck(bool? lOwned) => lOwned == true && !LNameplateDeveloper;

    public static bool LNameplateOwnerCheck(string? lName, string? lNamespace) =>
        !string.IsNullOrEmpty(lName)
        || (lNamespace is not null && lNamespace.StartsWith(LNameplateRoot, StringComparison.Ordinal));

    public static string LNameplateResolve(string? lName, string lType, string? lOwnerName, string? lOwnerType)
    {
        if (!string.IsNullOrEmpty(lName))
        {
            return lName;
        }

        if (!string.IsNullOrEmpty(lOwnerName))
        {
            return lOwnerName + LNameplateJoin + lType;
        }

        return lOwnerType is null ? lType : lOwnerType + LNameplateJoin + lType;
    }

    public static string LNameplateOwnerRead(string lResolved)
    {
        int lOwnerEnd = lResolved.LastIndexOf(LNameplateJoin, StringComparison.Ordinal);
        return lOwnerEnd > 0 ? lResolved[..lOwnerEnd] : string.Empty;
    }
}
