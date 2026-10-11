namespace Cadroue.ShellEngine;

public sealed class LUsherOutlet : LUsherPort
{
    public bool LUsherFileExist(string? lPath) => LUsherEngine.LUsherFileSeam(lPath);

    public bool LUsherFolderExist(string? lFolder) => LUsherEngine.LUsherFolderSeam(lFolder);

    public string LUsherNameRead(string lPath) => LUsherEngine.LUsherNameSeam(lPath);

    public string LUsherStemRead(string lPath) => LUsherEngine.LUsherStemSeam(lPath);

    public string LUsherExtensionRead(string lPath) => LUsherEngine.LUsherExtensionSeam(lPath);

    public string LUsherProgramResolve(string lName) => LUsherEngine.LUsherProgramSeam(lName);

    public string LUsherPathResolve(string lPath) => LUsherEngine.LUsherPathSeam(lPath);

    public string? LUsherPathOpen(string lPath, string lLabel, string? lFallback) =>
        LUsherEngine.LUsherRevealSeam(lPath, lLabel, lFallback);

    public string? LUsherFolderOpen(string lFolder, string lLabel) =>
        LUsherEngine.LUsherExplorerSeam(lFolder, lLabel);

    public string? LUsherLinkOpen(string lTarget, string lLabel) => LUsherEngine.LUsherLinkSeam(lTarget, lLabel);
}
