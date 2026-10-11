# TFlowLosslesscut.cs
Hash: `fbeeae3a2a006109`

## `public sealed class TFlowLosslesscut`

Checks the flow LosslessCut import sequence and its prompts without any control.
Prompts reach the test as one notice that carries an answer callback.
Each test with media builds a flow with a 60 second source and project files beside it.

## `public void Run_ReplacesTheSections_WhenPrimaryIsChosen()`

When sections already exist, the user is asked once whether to replace or append.
The primary answer replaces them with the imported segments.

## `public void Run_AppendsOrCancels_ByTheChoice()`

A cancelled choice leaves the sections unchanged.
The second answer appends the imported segments to the existing ones.

## `public void Run_AsksOnMediaMismatch_AndWarnsOnUnreadable()`

A project that names other media needs the user to confirm before import.
Declining imports nothing.
A file that cannot be read raises a warning instead.

## `public void Run_NoticesWhenNoMediaIsLoaded()`

Without loaded media there is nothing to import into.
The user gets a plain notice.

## `public void Find_OffersEachSibling_AndImportsTheAcceptedOne()`

Every sibling project file is offered in turn.
The first accepted one is imported and the search stops.

## `public void Find_DoesNothingWhileNotEditable()`

A flow that is not editable must not look for project files or ask anything.
