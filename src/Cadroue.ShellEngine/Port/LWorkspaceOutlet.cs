namespace Cadroue.ShellEngine;

// The engine's face for the workspace gate: every member forwards to the same-named member of the engine's
// workspace facade. It rejects a null engine and keeps it for every forward.
public sealed class LWorkspaceOutlet : LWorkspacePort
{
    private readonly LEngine _lWorkspaceOutletEngine;

    public LWorkspaceOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lWorkspaceOutletEngine = lEngine;
    }

    public (bool LWorkspaceLoadCancelled, bool LWorkspaceMediaClosed) LWorkspaceRemovalRead(
        IReadOnlyCollection<string> lRemoved, string? lIntent, string? lSource) =>
        _lWorkspaceOutletEngine.LEngineWorkspace.LWorkspaceRemovalRead(lRemoved, lIntent, lSource);

    public (bool LWorkspaceMediaClosed, bool LWorkspaceLoadCancelled, bool LWorkspaceMediaCleared) LWorkspaceClearRead(
        IReadOnlyCollection<string> lProtected, string? lSource, string? lIntent, bool lLoaded) =>
        _lWorkspaceOutletEngine.LEngineWorkspace.LWorkspaceClearRead(lProtected, lSource, lIntent, lLoaded);

    public string LWorkspacePathResolve(string lPath) =>
        _lWorkspaceOutletEngine.LEngineWorkspace.LWorkspacePathResolve(lPath);
}
