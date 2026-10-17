using System;

namespace Cadroue.Conduct;

// The sections' one pending name edit: the row its editor holds, the edit's serial and the text as typed, and when
// the edit is committed or dropped. CSection makes one and answers the editable switch, the name set the commit runs
// and the rows' rebuild.
public sealed class CSectionEdit
{
    private readonly CSection _cSectionEditOwner;
    private int? _cSectionEditIndex;
    private int _cSectionEditSerial;
    private string _cSectionEditName = string.Empty;
    private string _cSectionEditPrefix = string.Empty;
    private string _cSectionEditSuffix = string.Empty;

    internal CSectionEdit(CSection owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _cSectionEditOwner = owner;
    }

    // The row in the editor; null while none is edited.
    public int? CSectionEditIndex => _cSectionEditIndex;

    // Grows with each edit start, so an editor told of an older edit leaves it alone.
    public int CSectionEditSerial => _cSectionEditSerial;

    // The editor's text as typed, held until the edit is committed or dropped.
    public void CSectionEditSet(string name, string prefix, string suffix)
    {
        _cSectionEditName = name;
        _cSectionEditPrefix = prefix;
        _cSectionEditSuffix = suffix;
    }

    // The pending edit, trimmed, names its section; with no edit nothing runs.
    public void CSectionEditCommit()
    {
        if (_cSectionEditIndex is not int index)
        {
            return;
        }

        _cSectionEditIndex = null;
        _cSectionEditOwner.CSectionNameSet(
            _cSectionEditOwner.CSectionEditable,
            index,
            _cSectionEditName.Trim(),
            _cSectionEditPrefix.Trim(),
            _cSectionEditSuffix.Trim());
        _cSectionEditOwner.CSectionRowsRaise();
    }

    public void CSectionEditCancel()
    {
        _cSectionEditIndex = null;
        _cSectionEditOwner.CSectionRowsRaise();
    }

    // Handles the editor losing focus: only the live editor, with focus outside its boxes, commits.
    public void CSectionEditHandle(int serial, bool focusInside)
    {
        if (serial != _cSectionEditSerial || focusInside)
        {
            return;
        }

        CSectionEditCommit();
    }

    // The editor opens on a row with the row's text.
    internal void CSectionEditStart(int index, string name, string prefix, string suffix)
    {
        _cSectionEditIndex = index;
        _cSectionEditSerial++;
        _cSectionEditName = name;
        _cSectionEditPrefix = prefix;
        _cSectionEditSuffix = suffix;
        _cSectionEditOwner.CSectionRowsRaise();
    }
}
