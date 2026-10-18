using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// Every source's checkup results, one per flaw kind, and each scanning source's progress. Sources match ignoring case
// and kinds by name. CClinic makes one and tells a change when it touches the shown source.
internal sealed class CClinicCheckup
{
    private readonly Dictionary<string, Dictionary<string, (LCheckupPort CClinicResult, bool CClinicScanning)>>
        _cClinicCheckupResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _cClinicCheckupProgress = new(StringComparer.OrdinalIgnoreCase);

    internal double CClinicProgressRead(string? source) =>
        source is not null && _cClinicCheckupProgress.TryGetValue(source, out double value) ? value : 0;

    internal (LCheckupPort CClinicResult, bool CClinicScanning)? CClinicResultFind(string? source, string? kind) =>
        kind is not null
        && source is not null
        && _cClinicCheckupResults.TryGetValue(source, out Dictionary<string, (LCheckupPort, bool)>? kinds)
        && kinds.TryGetValue(kind, out (LCheckupPort, bool) stored)
            ? stored
            : null;

    // A result replaces its source's result of that kind; a scan restarts the progress, any other outcome ends it.
    // The result's source and kind are answered.
    internal (string CClinicSource, string CClinicKind) CClinicResultAccept(LCheckupPort result)
    {
        ArgumentNullException.ThrowIfNull(result);
        (string source, string kind, bool scanning) = result.LCheckupRead();
        if (!_cClinicCheckupResults.TryGetValue(source, out Dictionary<string, (LCheckupPort, bool)>? kinds))
        {
            kinds = new Dictionary<string, (LCheckupPort, bool)>(StringComparer.Ordinal);
            _cClinicCheckupResults[source] = kinds;
        }

        kinds[kind] = (result, scanning);
        if (scanning)
        {
            _cClinicCheckupProgress[source] = 0;
        }
        else
        {
            _cClinicCheckupProgress.Remove(source);
        }

        return (source, kind);
    }

    // The path's results and progress go; whether it held results is answered.
    internal bool CClinicResultRemove(string path)
    {
        bool removed = _cClinicCheckupResults.Remove(path);
        _cClinicCheckupProgress.Remove(path);
        return removed;
    }

    internal void CClinicProgressSet(string path, double value) =>
        _cClinicCheckupProgress[path] = Math.Clamp(value, 0, 1);
}
