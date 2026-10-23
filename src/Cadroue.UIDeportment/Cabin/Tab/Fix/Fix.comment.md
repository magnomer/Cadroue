# Fix

## `QFixCatalog`

The catalog pairs each supported flaw kind with its processing row, then appends the salvage row.
The kind-to-row pairing lets diagnosis request exactly the same flaw kinds that the processing surface offers.

## `QFixTab`

`QFixTab` owns the Fix tab's clinic gate, clinic adapter, checkup, face adapter and long-lived workspace subscriptions.
Construction initializes processing as unordered and attaches plan, list, viewer, drop, processing and diagnosis publishers.

Close detaches the face first, removes every subscription installed by `QFixAttach`, then disposes the checkup.
This symmetry keeps callbacks from workspace publishers out of a closed Fix tab.

Run requests delegate preset validation to `CFix`.
A rejected request raises `QFixPresetMissing`.
Diagnosis silently returns when no unlocked listed item is selected.
Deferred result and progress callbacks update the clinic only while their source remains in the docket.

## `QFixFace`

The face owns checkup-ready, checkup-progress and missing-preset presentation subscriptions.
Each attachment captures its own revocable continuation for dispatcher work.
It subscribes the new checkup and notice, then stores symmetric removal actions.

Detach revokes that attachment's continuation before removing its subscriptions, then forgets the notice.
Ready and progress events are deferred through the supplied dispatcher.
They reach `QFixTab` only while the attachment that queued them remains current.

## Stamp

Commit: `9a34a722aa84`
- `QFixCatalog.cs` `266ea00669968d58`
- `QFixFace.cs` `f9ccaed65daea28b`
- `QFixTab.cs` `17c4b2f09074d737`
