namespace Cadroue.ShellEngine;

// The engine's face for the usher gate: every member forwards to the same-named member of the engine's usher
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LUsherOutlet : LUsherPort
{
    private readonly LEngine _lUsherOutletEngine;

    public LUsherOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lUsherOutletEngine = lEngine;
    }

    public bool LUsherFileExist(string? lPath) => _lUsherOutletEngine.LEngineUsher.LUsherFileExist(lPath);

    public bool LUsherFolderExist(string? lFolder) => _lUsherOutletEngine.LEngineUsher.LUsherFolderExist(lFolder);

    public string LUsherNameRead(string lPath) => _lUsherOutletEngine.LEngineUsher.LUsherNameRead(lPath);

    public string LUsherStemRead(string lPath) => _lUsherOutletEngine.LEngineUsher.LUsherStemRead(lPath);

    public string LUsherProgramResolve(string lName) => _lUsherOutletEngine.LEngineUsher.LUsherProgramResolve(lName);

    public string? LUsherPathOpen(string lPath, string? lFallback, string? lFailure) =>
        _lUsherOutletEngine.LEngineUsher.LUsherPathOpen(lPath, lFallback, lFailure);

    public string? LUsherFolderOpen(string lFolder) => _lUsherOutletEngine.LEngineUsher.LUsherFolderOpen(lFolder);

    public string? LUsherLinkOpen(string lTarget, string? lFailure) =>
        _lUsherOutletEngine.LEngineUsher.LUsherLinkOpen(lTarget, lFailure);
}
