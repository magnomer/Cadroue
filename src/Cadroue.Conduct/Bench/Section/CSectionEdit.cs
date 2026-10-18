using System;

namespace Cadroue.Conduct;

// The sections' one pending name edit: the row its editor holds, the edit's serial and the text as typed, and when
// the edit is committed or dropped. CSectionList makes one and answers the rows' rebuild; CSection answers the
// editable switch and the name set the commit runs.
public sealed class CSectionEdit
{
    private readonly CSection _cSectionEditSection;
    private readonly CSectionList _cSectionEditList;
    private int? _cSectionEditIndex;
    private int _cSectionEditSerial;
    private string _cSectionEditName = string.Empty;
    private string _cSectionEditPrefix = string.Empty;
    private string _cSectionEditSuffix = string.Empty;

    internal CSectionEdit(CSection section, CSectionList list)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(list);
        _cSectionEditSection = section;
        _cSectionEditList = list;
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
        _cSectionEditSection.CSectionNameSet(
            _cSectionEditSection.CSectionEditable,
            index,
            _cSectionEditName.Trim(),
            _cSectionEditPrefix.Trim(),
            _cSectionEditSuffix.Trim());
        _cSectionEditList.CSectionRowsRaise();
    }

    public void CSectionEditCancel()
    {
        _cSectionEditIndex = null;
        _cSectionEditList.CSectionRowsRaise();
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
        _cSectionEditList.CSectionRowsRaise();
    }
}
