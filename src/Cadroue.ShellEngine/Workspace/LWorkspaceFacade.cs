namespace Cadroue.ShellEngine;

public sealed class LWorkspaceFacade
{
    internal LWorkspaceFacade()
    {
    }

    public (bool LWorkspaceLoadCancelled, bool LWorkspaceMediaClosed) LWorkspaceRemovalRead(
        IReadOnlyCollection<string> lRemoved, string? lIntent, string? lSource) =>
        (lIntent is not null && lRemoved.Contains(lIntent, StringComparer.OrdinalIgnoreCase),
            lSource is not null && lRemoved.Contains(lSource, StringComparer.OrdinalIgnoreCase));

    public (bool LWorkspaceMediaClosed, bool LWorkspaceLoadCancelled, bool LWorkspaceMediaCleared) LWorkspaceClearRead(
        IReadOnlyCollection<string> lProtected, string? lSource, string? lIntent, bool lLoaded)
    {
        HashSet<string> lKept = lProtected.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool lIntentKept = lIntent is not null && lKept.Contains(lIntent);
        bool lHeld = (lSource is not null && lKept.Contains(lSource)) || lIntentKept;
        bool lCancelled = lHeld && lIntent is not null && !lIntentKept;
        return (!lHeld, lCancelled, (!lHeld && lLoaded) || lCancelled);
    }

    // The full load path; a path the system cannot resolve loads as given.
    public string LWorkspacePathResolve(string lPath) =>
        string.IsNullOrWhiteSpace(lPath) ? string.Empty : LUsherEnvironmentEngine.LUsherPathSeam(lPath);
}
