using System;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QExportRows(
    Func<string, string?, CExportRowState> row,
    Func<string?> selected,
    Func<string, bool> editing,
    Func<string, string> display,
    string notice)
{
    public QExportRows(
        CExportRow row,
        LPresetDocument working,
        Func<string?> selected,
        Func<string, bool> editing,
        Func<string, string> display,
        string notice)
        : this(
            (name, value) => row.CExportRowRead(name, value, working),
            selected,
            editing,
            display,
            notice)
    {
    }

    public QExportFace QExportRowsRead(string name)
    {
        CExportRowState state = row(name, selected());
        return new(
            display(name),
            state.CExportRowNative,
            state.CExportRowSelected,
            state.CExportRowModified,
            state.CExportRowSupported,
            editing(name),
            notice);
    }
}
