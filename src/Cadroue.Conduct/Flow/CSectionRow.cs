using System;
using System.Collections.Generic;

namespace Cadroue.Conduct;

// One painted section row as the gate answers it: the name and affixes with whether each is set, the set affixes in
// order, the span held at zero or more, the colour, and whether the row is hidden, selected and in the editor.
public sealed record CSectionRow(
    int CSectionRowIndex,
    string CSectionRowName,
    bool CSectionRowUnnamed,
    string CSectionRowPrefix,
    string CSectionRowSuffix,
    bool CSectionRowPrefixed,
    bool CSectionRowSuffixed,
    IReadOnlyList<string> CSectionRowAffixes,
    TimeSpan CSectionRowOrigin,
    TimeSpan CSectionRowEnd,
    TimeSpan CSectionRowSpan,
    int CSectionRowColor,
    bool CSectionRowHidden,
    bool CSectionRowSelected,
    bool CSectionRowEditing);
