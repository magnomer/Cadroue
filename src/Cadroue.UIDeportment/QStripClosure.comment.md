# QStripClosure.cs
Hash: `7c793a6eabddf157`

## `public sealed class QStripClosure`

Owns the tab close path: the busy question, the confirm, one close and close all.
It shares the owner's tab collection and removes tabs through the owner.
The busy check stays on the owner, so the question reads it through the owner.

## `public QStripClosure(QStrip lOwner, ObservableCollection<QStripTab> lTabs, Func<string, string> lLocalization, Action<string> lTrace, Action<Guid> lCartographer)`

Takes the text read, the trace record and the stage remove as delegates from the owner.
So this file names no engine type, and the owner keeps those dependencies in one place.

## `public event Action<QStripTab>? QStripTabClosed`

Raised before a tab leaves the strip, so subscribers can still reach its workspace.

## `public QAsk? QStripCloseResolve(QStripTab? lTab = null)`

Builds the stop question when a close would interrupt running work, else returns null.

## `public bool QStripCloseConfirm(QStripTab? lTab = null)`

Asks the question and traces a declined close.
A null question answers yes at once, so an idle close never shows a modal.

## `public void QStripClose(QStripTab lTab)`

Closes one tab: raises the notice, drops its cartographer stage, removes it and traces the count.

## `public void QStripAllClose()`

Raises the close notice and drops the stage for every tab, then empties the strip in one step.
