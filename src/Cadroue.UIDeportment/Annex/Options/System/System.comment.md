# System

## QSOptionsOperation.cs

This helper only projects progress, the installer button and the busy state.
It owns no identity.
The initiating driver keeps the instance it created and compares it with the current one.

## QSFlyleaf.cs

An install holds the operation object it started with.
Detach clears the current operation, so every late progress, completion and notice finds a mismatch and does nothing.
A reattach creates a new operation, so the old install cannot write to the replacement surface.
Test `Start_DetachBeforeCompletion_DoesNotUpdateSurface` pins the detach.
Test `Reattach_DoesNotRedirectActiveOperation` pins the reattach.

A failed install rethrows only while still attached.
Test `Start_AttachedFailure_PreservesException` pins the rethrow.
Attach always detaches first, so a second attach never doubles a handler.
Test `Reattach_DoesNotDuplicateHandlers` pins one handler per click.

## QSFlyleafAnswer.cs

The answer holds the open, announcement and warning callbacks.
The callbacks sit in a private array behind typed members, so the driver never casts.
A test builds the answer from plain actions, and the window builds it from an owner and its message windows.
The success flag picks the announcement or the warning.

## QSMpv.cs

The driver also owns the mpv availability and raises `QSMpvChanged` each time it is set, even to the same value.
The Veneer draft subscribes when the window is built and unsubscribes at close.
The install restores the installer and hides the progress bar before the availability check begins.
Test `Mpv_RestoresIdleBeforeAvailability` pins that order.
The availability result is applied only while the operation is still current.
Test `Start_ReattachBeforeCompletion_DoesNotUpdateReplacementSurface` pins it.
Test `Start_DetachBeforeCompletion_DoesNotUpdateSurface` pins the silence after detach.
A failed availability check is traced and counted as unavailable, so the window never sees an exception.

## QSMpvAnswer.cs

The answer has the same shape as the Flyleaf one.
Only the folder it opens differs.

## QSRenderer.cs

The driver keeps the typed path itself, apart from the surface.
Detach drops the controls but keeps the path, so a reattach restores what the user typed.
Apply returns the typed path as written, even when it is blank.
Test `QSRendererAttach` pins that.
The status key comes from the gate's state through one table.

## QSRendererAnswer.cs

The answer holds the folder dialog, the open action and the diagnosis window.
The folder dialog returns null on cancel, and the driver then leaves the path as it is.

## QSWorkspace.cs

The driver is the only handler and lifetime owner of the workspace surface.
Conduct decides every step and this driver only shows the result.
A dialog or confirmation can finish after the surface detached, so each continuation compares the answer it started with.
Detach clears the current answer, so a late continuation does nothing.
The comparison assumes a new answer object for each attach, which the window provides.
Test `Detach_PreventsSurfaceCallbacks` pins the silence.

The refresh is split in two.
`QSWorkspaceUpdate` refreshes the applied state and the default notice.
`QSWorkspaceSizeUpdate` refreshes the size.
Attach runs the size first, then the state.
The System page refreshes the state after apply and the size after clearing sidecar records.
Tests `Workspace_AppliedRefresh_KeepsSize` and `Workspace_SidecarRefresh_KeepsState` pin the split.

Done clear sends no clear request at an unapplied folder or after a rejected confirmation.
Tests `DoneClear_Unapplied_DoesNotRequest` and `DoneClear_Rejected_DoesNotRequest` pin both stops.
Workspace clear warns without confirming while work runs.
Test `WorkspaceClear_Running_ShowsWarningOnly` pins that.
Both clears confirm, clear, read the size and announce, in that order.
Tests `DoneClear_Accepted_PreservesEffectOrder` and `WorkspaceClear_Accepted_PreservesEffectOrder` pin the order.
A cancelled folder dialog leaves the path as it was.
Test `Browse_Cancelled_PreservesPath` pins that.

The commit restores the kept folder in the box and warns when the gate reports an occupied destination.
Apply continues after the warning.
Sizes show bytes as integers and larger units with at most two decimals, using 1024 steps.

## QSWorkspaceAnswer.cs

The answer holds the folder dialog, confirmation, warning and announcement.
The folder dialog and the confirmation take continuations, so a test can finish them late.
A confirmation runs its continuation only when accepted.

## Stamp

Commit: `b84e5458de50`
- `QSFlyleaf.cs` `9e410efce6b343fd`
- `QSFlyleafAnswer.cs` `da8de300f659c5fd`
- `QSMpv.cs` `81a9237086bca6ac`
- `QSMpvAnswer.cs` `389550663c53a804`
- `QSOptionsOperation.cs` `f722589cbac476c5`
- `QSRenderer.cs` `3dd42e121e8e32c2`
- `QSRendererAnswer.cs` `efc947956ecbf335`
- `QSWorkspace.cs` `dd8e3e280bafd465`
- `QSWorkspaceAnswer.cs` `60e19d0543b78297`
