# QRosterSelection.cs
Hash: `a23a1f6400e43cab`

## `public sealed class QRosterSelection`

Owns which steps and which batch card the user has selected in the roster.
Its only writers are the two select requests, so a schedule change never writes it.
Ids whose work has left the roster stay stored but every read ignores them.

## `public QRosterSelection(`

Takes the owner's present step check, present batch check and step order as delegates.
The last delegate is the owner's selection notice, run once after each request.

## `public Guid LRosterCurrentId =>`

Answers the current step only while it is present, else the empty id.

## `public Guid LRosterCardId =>`

Answers the selected batch only while it is present, else the empty id.

## `public IReadOnlyCollection<Guid> LRosterSelectedIds =>`

Lists the selected steps that are present now.

## `public bool LRosterSelectedCheck(Guid lId)`

Reports a step as selected only while it is present, so a removed job never shows selected.

## `public bool LRosterCardCheck(Guid lBatchId)`

Reports whether a batch is the selected card, through the present-only card read.

## `public void LRosterStepSelect(Guid lId, bool lRange, bool lToggle)`

Drops stale ids as part of its own write, then replaces the whole pick at once.
Selecting a step always clears the card selection.

## `private static QRosterPick LRosterPickResolve(`

Computes the next pick from the present pick, the step order and the request alone.
A range spans from the current step to the target and keeps the current step.
A toggle flips one step and makes it current, a plain select replaces the set.
A range whose anchor or target is missing from the order leaves the set unchanged.
Without a present current step, a range request acts as a toggle or plain select.

## `public void LRosterCardSelect(Guid lBatchId)`

Selects one batch card and clears the step selection and current step.
