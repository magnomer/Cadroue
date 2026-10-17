using System.Globalization;

namespace Cadroue.ShellEngine;

public sealed class LUsherFacade
{
    internal LUsherFacade()
    {
    }

    public bool LUsherFileExist(string? lPath) => LUsherDiskEngine.LUsherFileSeam(lPath);

    public bool LUsherFolderExist(string? lFolder) => LUsherDiskEngine.LUsherFolderSeam(lFolder);

    public string LUsherNameRead(string lPath) => LUsherTextEngine.LUsherNameSeam(lPath);

    public string LUsherStemRead(string lPath) => LUsherTextEngine.LUsherStemSeam(lPath);

    public string LUsherProgramResolve(string lName) => LUsherEnvironmentEngine.LUsherProgramSeam(lName);

    public string? LUsherPathOpen(string lPath, string? lFallback, string? lFailure) =>
        LUsherFailureRecord(lPath, LUsherEngine.LUsherRevealSeam(lPath, lFallback), lFailure);

    public string? LUsherFolderOpen(string lFolder) => LUsherEngine.LUsherExplorerSeam(lFolder);

    public string? LUsherLinkOpen(string lTarget, string? lFailure) =>
        LUsherFailureRecord(lTarget, LUsherEngine.LUsherLinkSeam(lTarget), lFailure);

    private static string? LUsherFailureRecord(string lTarget, string? lError, string? lFailure)
    {
        if (lError is not null && lFailure is not null)
        {
            LTraceEngine.LTraceErrorSeam(string.Format(CultureInfo.InvariantCulture, lFailure, lTarget, lError), null);
        }

        return lError;
    }
}
