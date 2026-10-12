namespace Cadroue.ShellEngine;

public interface LUsherPort
{
    bool LUsherFileExist(string? lPath);

    bool LUsherFolderExist(string? lFolder);

    string LUsherNameRead(string lPath);

    string LUsherStemRead(string lPath);

    string LUsherProgramResolve(string lName);

    string? LUsherPathOpen(string lPath, string? lFallback, string? lFailure);

    string? LUsherFolderOpen(string lFolder);

    string? LUsherLinkOpen(string lTarget, string? lFailure);
}
