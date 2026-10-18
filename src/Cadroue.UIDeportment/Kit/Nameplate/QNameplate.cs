using Cadroue.Application;

namespace Cadroue.UIDeportment;

public static class QNameplate
{
    private const string QNameplateJoin = " › ";
    private const string QNameplateRoot = "Cadroue";

    public static bool QNameplateDeveloper => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public static bool QNameplateOwnedResolve(object? lTooltip, bool? lOwned)
    {
        bool lOwnedNow = lOwned ?? false;
        if (!QNameplateDeveloper || (lTooltip is not null && !lOwnedNow))
        {
            return lOwnedNow;
        }

        return true;
    }

    public static object? QNameplateTipResolve(bool? lOwned, object? lTooltip, Func<string> lResolve) =>
        QNameplateDeveloper && lOwned == true ? lResolve() : lTooltip;

    public static bool QNameplateTipCheck(bool? lOwned) => lOwned == true && !QNameplateDeveloper;

    public static bool QNameplateOwnerCheck(string? lName, string? lNamespace) =>
        !string.IsNullOrEmpty(lName)
        || (lNamespace is not null && lNamespace.StartsWith(QNameplateRoot, StringComparison.Ordinal));

    public static string QNameplateResolve(string? lName, string lType, string? lOwnerName, string? lOwnerType)
    {
        if (!string.IsNullOrEmpty(lName))
        {
            return lName;
        }

        if (!string.IsNullOrEmpty(lOwnerName))
        {
            return lOwnerName + QNameplateJoin + lType;
        }

        return lOwnerType is null ? lType : lOwnerType + QNameplateJoin + lType;
    }

    public static string QNameplateOwnerRead(string lResolved)
    {
        int lOwnerEnd = lResolved.LastIndexOf(QNameplateJoin, StringComparison.Ordinal);
        return lOwnerEnd > 0 ? lResolved[..lOwnerEnd] : string.Empty;
    }
}
