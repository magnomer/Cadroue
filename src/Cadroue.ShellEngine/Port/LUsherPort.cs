namespace Cadroue.ShellEngine;

public interface LUsherPort
{
    bool LUsherFileExist(string? lPath);

    bool LUsherFolderExist(string? lFolder);

    string LUsherNameRead(string lPath);

    string LUsherStemRead(string lPath);

    string LUsherExtensionRead(string lPath);

    string LUsherProgramResolve(string lName);

    string LUsherPathResolve(string lPath);

    string? LUsherPathOpen(string lPath, string lLabel, string? lFallback);

    string? LUsherFolderOpen(string lFolder, string lLabel);

    string? LUsherLinkOpen(string lTarget, string lLabel);
}
