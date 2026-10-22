using System;
using Cadroue.Core;

namespace Cadroue.Conduct;

public readonly record struct CExportRowState(
    bool CExportRowNative,
    bool CExportRowSelected,
    bool CExportRowModified,
    bool CExportRowSupported);

public sealed class CExportRow(
    Func<string, bool> native,
    Func<string, LPresetDocument, bool> matched,
    Func<string, LPresetDocument, bool, bool> supported)
{
    public CExportRowState CExportRowRead(string name, string? selected, LPresetDocument working)
    {
        bool isNative = native(name);
        bool isSelected = string.Equals(name, selected, StringComparison.OrdinalIgnoreCase);
        bool isMatched = isNative || !isSelected || matched(name, working);
        return new(
            isNative,
            isSelected,
            !isNative && isSelected && !isMatched,
            supported(name, working, isSelected));
    }
}
