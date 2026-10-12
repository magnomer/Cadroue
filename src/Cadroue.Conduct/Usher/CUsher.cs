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

    public (string CUsherPath, string? CUsherFailure) CUsherProgramOpen(string name, string? failure = null)
    {
        string path = _cUsherPort.LUsherProgramResolve(name);
        return (path, CUsherLinkOpen(path, failure));
    }

    public string? CUsherPathOpen(string path, string? fallback = null, string? failure = null) =>
        _cUsherPort.LUsherPathOpen(path, fallback, failure);

    public string? CUsherFolderOpen(string folder) => _cUsherPort.LUsherFolderOpen(folder);

    public string? CUsherLinkOpen(string target, string? failure = null) =>
        _cUsherPort.LUsherLinkOpen(target, failure);
}
