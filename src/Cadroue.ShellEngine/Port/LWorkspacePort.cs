namespace Cadroue.ShellEngine;

public interface LWorkspacePort
{
    (bool LWorkspaceLoadCancelled, bool LWorkspaceMediaClosed) LWorkspaceRemovalRead(
        IReadOnlyCollection<string> lRemoved, string? lIntent, string? lSource);

    (bool LWorkspaceMediaClosed, bool LWorkspaceLoadCancelled, bool LWorkspaceMediaCleared) LWorkspaceClearRead(
        IReadOnlyCollection<string> lProtected, string? lSource, string? lIntent, bool lLoaded);

    string LWorkspacePathResolve(string lPath);
}
