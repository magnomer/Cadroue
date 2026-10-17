using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's section rename: which section the open rename shows, with that section's names, and the trimmed names
// it keeps on that section. CFlow makes one per flow; the section gate decides whether the names may be kept.
public sealed class CFlowName
{
    private readonly LFlowPort _cFlowNamePort;
    private readonly CSection _cFlowNameSection;

    internal CFlowName(LFlowPort port, CSection section)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(section);
        _cFlowNamePort = port;
        _cFlowNameSection = section;
    }

    public event Action? CFlowNameShown;

    public event Action? CFlowNameClosed;

    // The section the open rename shows; null while no rename is open.
    public int? CFlowNameIndex { get; private set; }

    public string CFlowNameText { get; private set; } = string.Empty;

    public string CFlowNamePrefix { get; private set; } = string.Empty;

    public string CFlowNameSuffix { get; private set; } = string.Empty;

    public bool CFlowPrefixShown => !string.IsNullOrEmpty(CFlowNamePrefix);

    public bool CFlowSuffixShown => !string.IsNullOrEmpty(CFlowNameSuffix);

    // The selected section's rename: an open one closes first, then the section's names show.
    public bool CFlowNameStart()
    {
        if (_cFlowNameSection.CSectionSelection is not int index
            || _cFlowNamePort.LFlowNameRead(_cFlowNameSection.CSectionSegment, index) is not { } names)
        {
            return false;
        }

        CFlowNameHide();
        (CFlowNameText, CFlowNamePrefix, CFlowNameSuffix) = names;
        CFlowNameIndex = index;
        CFlowNameShown?.Invoke();
        return true;
    }

    // The open rename keeps its names trimmed on its section, as the editable switch lets it, and closes.
    public void CFlowNameCommit(bool editable, string name, string prefix, string suffix)
    {
        if (CFlowNameIndex is int index)
        {
            _cFlowNameSection.CSectionNameSet(editable, index, name.Trim(), prefix.Trim(), suffix.Trim());
        }

        CFlowNameHide();
    }

    public void CFlowNameHide()
    {
        if (CFlowNameIndex is null)
        {
            return;
        }

        CFlowNameIndex = null;
        CFlowNameClosed?.Invoke();
    }
}
