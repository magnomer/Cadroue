# QStrip.cs
Hash: `be78245883457225`

## `public sealed class QStrip`

Owns the ordered tab list, the selection, tab titles and the relay reads.
It constructs its separator and close siblings over the same tab collection.

## `public QStripSeparator QStripSeparator { get; }`

The hover, orientation and separator sibling.

## `public QStripClosure QStripClosure { get; }`

The close path sibling, which also raises the tab closed notice.

## `public static string QStripKeyResolve(string lKey)`

Maps an unknown tab key to the Split key, so a stale scene never opens a nameless mode.

## `public QStripTab? QStripTabFind(LWorkItem lWorkItem)`

Finds the tab a relayed item came from.
A relay stage id is mapped back to the tab that first owned the stage.

## `public void QStripRelayAttach()`

Fills the relay engine's tab seams with this strip's reads and drain.

## `public IReadOnlyList<QStripTab> QStripRelayRead(Guid lSourceTab)`

Lists the tabs a relay may target: every other tab that holds a docket.

## `public IReadOnlyList<LCartographerTab> QStripCartographerRead()`

Hands the cartographer a copy of each tab that has both a preset and a layout.
Tabs without either cannot take part in a relay plan.

## `public IReadOnlyList<LSealNode> QStripSealRead()`

Hands the seal one node per docket tab, with the cohorts already delivered to it.

## `public void QStripUpdateSuspend()`

Counts a suspend depth, so a nested restore resumes only when the outer one ends.

## `public void QStripAdd(QStripTab lTab)`

Appends a tab, titles it and selects it when nothing is selected yet.
While updates are suspended, it sets only the plain title and leaves numbering and selection to the caller.

## `public bool QStripNameSet(QStripTab lTab, string? lName)`

Returns false when the name is empty or the standard name, which clears the custom name.
A custom name taken by another tab is made unique first.

## `public void QStripRemove(QStripTab lTab)`

Removes a tab and selects its neighbour when it was the selected one.
The hover is forgotten first, so separators never read the removed index.

## `public bool QStripBusyCheck(QStripTab? lTab = null)`

Answers whether one tab, or any tab when none is given, still has work running.
The close sibling asks it before building the stop question.

## `public bool QStripContentClear()`

Clears every tab's media and stale paths, sparing cohorts that running work still needs.
The protected set is read once, so every tab is judged against the same answer.

## `public void QStripTabRaise(QStripTab lTab)`

Refreshes a tab's bindings and raises its change notice.
The separator sibling calls it too, so every tab change leaves through one door.
