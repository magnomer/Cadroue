using System.Collections.Generic;

namespace Cadroue.Conduct;

// One card of the list: an unlocked path alone, or a locked batch's paths together.
public sealed record CListCard(bool CListCardLocked, IReadOnlyList<CListRow> CListCardRows);

// One path's row: the name the usher shows, whether it is locked or selected, and whether another row of its card
// follows (always set on a row alone).
public sealed record CListRow(
    string CListRowPath, string CListRowName, bool CListRowLocked, bool CListRowSelected, bool CListRowLast);
