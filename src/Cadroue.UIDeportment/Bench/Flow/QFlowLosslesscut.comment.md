# QFlowLosslesscut.cs
Hash: `e2b23f500cd06390`

## `public enum QFlowLosslesscutKind`

Names which modal the veneer shows for a prompt.
Decision asks for proceed or cancel, choice offers two actions, notice and warning dismiss only.

## `public sealed record QFlowLosslesscutPrompt(`

Carries the localized texts of one modal so the veneer needs no catalog access.
Unused action labels are empty strings.

## `public sealed class QFlowLosslesscut`

Owns the LosslessCut project find and import sequence and every prompt it raises.
It never shows a modal itself.

## `public event Action<QFlowLosslesscutPrompt, Action<int>>? QFlowLosslesscutAsk;`

The veneer answers synchronously through the callback with 0 for primary, 1 for alternate, -1 for dismiss.
With no subscriber the answer stays dismissed.

## `public void QFlowLosslesscutFind()`

Offers each project file found beside the source in turn and stops at the first one imported.
An unreadable project is traced and skipped.
Nothing happens when the tab is inactive, not editable or without a source.

## `public void QFlowLosslesscutRun(string lPath)`

Reads the project and confirms each risk before changing sections.
A tab that cannot edit sections gets a no-media notice instead of an import.
An old version and a media mismatch each need a confirmation.
An import with no valid section only reports a notice.
The final choice either replaces the section list or appends to it.

## `public static string QFlowLosslesscutFormat(string lPath, LLosslesscutResult lResult, bool lShowRange)`

Builds the multi-line summary shown in the prompts.
At most five skipped sections are listed, followed by a count of the rest.

## `private int QFlowLosslesscutRaise(QFlowLosslesscutPrompt lPrompt)`

Returns the veneer's answer, or dismiss when nobody answers.

## `private static QFlowLosslesscutPrompt QFlowNoticeCreate(`

Builds a single-dismiss prompt with a localized title and a ready message.

## `private static QFlowLosslesscutPrompt QFlowConfirmCreate(string lTitleKey, string lMessage, string lPrimaryKey) =>`

Builds a proceed-or-cancel prompt whose primary label comes from the given key.

## `private static string QFlowFallbackRead(string lValue, string lFallback) =>`

Substitutes the not-specified text for a blank project value.
