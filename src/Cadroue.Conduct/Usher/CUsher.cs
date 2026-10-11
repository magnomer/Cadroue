using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CUsher
{
    private readonly LUsherPort _cUsherPort;

    internal CUsher(LUsherPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cUsherPort = port;
    }

    public bool CUsherFileExist(string? path) => _cUsherPort.LUsherFileExist(path);

    public bool CUsherFolderExist(string? folder) => _cUsherPort.LUsherFolderExist(folder);

    public string CUsherNameRead(string path) => _cUsherPort.LUsherNameRead(path);

    public string CUsherStemRead(string path) => _cUsherPort.LUsherStemRead(path);

    public string CUsherExtensionRead(string path) => _cUsherPort.LUsherExtensionRead(path);

    public string CUsherProgramResolve(string name) => _cUsherPort.LUsherProgramResolve(name);

    public string CUsherPathResolve(string path) => _cUsherPort.LUsherPathResolve(path);

    public string? CUsherPathOpen(string path, string label, string? fallback = null) =>
        _cUsherPort.LUsherPathOpen(path, label, fallback);

    public string? CUsherFolderOpen(string folder, string label) => _cUsherPort.LUsherFolderOpen(folder, label);

    public string? CUsherLinkOpen(string target, string label) => _cUsherPort.LUsherLinkOpen(target, label);
}
