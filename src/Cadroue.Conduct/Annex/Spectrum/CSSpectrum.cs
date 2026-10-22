using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

public sealed class CSSpectrum
{
    public const string CSSpectrumDefault = "Cadroue";

    private readonly CSSpectrumPort _csSpectrumPort;
    private CSSpectrumSnapshot _csSpectrumSnapshot = new([]);
    private string _csSpectrumName;

    public CSSpectrum(string name, CSSpectrumPort removal)
    {
        ArgumentNullException.ThrowIfNull(removal);
        _csSpectrumName = name;
        _csSpectrumPort = removal;
    }

    public string CSSpectrumName => _csSpectrumName;

    public CSSpectrumSnapshot CSSpectrumSnapshot => _csSpectrumSnapshot;

    public static bool CSSpectrumFixedCheck(string name) =>
        string.Equals(name, CSSpectrumDefault, StringComparison.Ordinal);

    public void CSSpectrumNamesSet(IReadOnlyList<string> names)
    {
        bool kept = names.Contains(_csSpectrumName, StringComparer.Ordinal);
        CSSpectrumNameSet(CSSpectrumNameResolve(kept));
    }

    private void CSSpectrumNameSet(string name) => _csSpectrumName = name;

    private string CSSpectrumNameResolve(bool kept) => kept ? _csSpectrumName : CSSpectrumDefault;

    public CSSpectrumImportResult CSSpectrumImport(
        string sourcePath, out string targetPath, out string? loadedName)
    {
        CSSpectrumImportResult result =
            _csSpectrumPort.CSSpectrumPortImport(sourcePath, out targetPath, out loadedName);
        return result == CSSpectrumImportResult.CSSpectrumImportLoaded && loadedName is null
            ? CSSpectrumImportResult.CSSpectrumImportInvalid
            : result;
    }

    public CSSpectrumSnapshot CSSpectrumUpdate()
    {
        _csSpectrumSnapshot = _csSpectrumPort.CSSpectrumPortRead();
        CSSpectrumNamesSet(_csSpectrumSnapshot.CSSpectrumSnapshotRows
            .Select(row => row.CSSpectrumRowName)
            .ToArray());
        return _csSpectrumSnapshot;
    }

    public void CSSpectrumSave(string targetPath) =>
        _csSpectrumPort.CSSpectrumPortSave(targetPath, _csSpectrumName);

    public bool CSSpectrumSelect(string name)
    {
        if (string.Equals(_csSpectrumName, name, StringComparison.Ordinal))
        {
            return false;
        }

        _csSpectrumName = name;
        return true;
    }

    public bool CSSpectrumRemove(string name, bool native, string? path)
    {
        if (CSSpectrumFixedCheck(name) || !_csSpectrumPort.CSSpectrumPortRemove(name, native, path))
        {
            return false;
        }

        bool kept = !string.Equals(_csSpectrumName, name, StringComparison.Ordinal);
        CSSpectrumNameSet(CSSpectrumNameResolve(kept));
        return true;
    }
}
