# System

These drivers adapt Options decisions to folder dialogs, filesystem libraries and System presentation surfaces.
Each attachment owner removes the handlers it adds.
Asynchronous installers additionally bind updates to the operation instance that started them.

## QSFlyleaf.cs

`QSFlyleaf` drives the Flyleaf surface and implements the production `CSFlyleafPort`.
Attach first detaches an old surface, creates a new operation identity, projects installed state and subscribes installer and folder-opening handlers.

Start captures the current operation and offers its start projection to Conduct.
Progress, success and failure require that exact operation to remain current before updating its surface.
Completion hides progress, restores the install or reinstall label and enables the button.
It routes success to an announcement and failure to a warning.
An unexpected exception hides progress and is rethrown only while the operation remains attached.
Detaching makes late work silent.

Detach removes both handlers and clears the operation, callbacks, controls and answer boundary.
An older install therefore cannot update a replacement surface.
The production port delegates installation and reports installed state again after the attempt.

## QSFlyleafAnswer.cs

The answer boundary owns folder opening and success or failure presentation.
Production construction binds those actions to the Options folder opener and owning window.
The action constructor keeps tests or other hosts independent of WPF dialogs.

## QSMpv.cs

`QSMpv` mirrors the Flyleaf operation lifetime while separately owning mpv runtime availability.
It offers the captured operation's start projection to Conduct.
Setting availability stores the value and raises `QSMpvChanged`, including when the value did not change.
Availability failures are traced and projected as unavailable instead of escaping to the window.

An install completion first hides progress, restores and enables the installer, then refreshes availability.
The completion notice appears only after that refresh and only if the initiating operation is still current.
Detach invalidates all later progress, availability and notice presentation from that operation.

The production port distinguishes installed files from renderer availability and rereads installed state after installation.

## QSMpvAnswer.cs

The mpv answer boundary has the Flyleaf answer shape but opens the mpv installation root.
Its success flag selects announcement or warning presentation.

## QSOptionsOperation.cs

This helper projects one install operation onto an installer button and progress bar.
Start disables the button, resets progress to zero and shows it.
Callers decide when to hide progress, re-enable the button and alter its content.
Identity and lifetime remain with the initiating installer driver.

## QSOptionsSystem.cs

`QSOptionsSystem` coordinates workspace, renderer, cleanup and file-record controls around one Conduct owner.
Attach first detaches the previous attachment and creates a new pending-clear token.
It attaches workspace and renderer children and projects the current snapshot.
Its deferred reader returns record location, cleanup state and days, and renderer path.
Cleanup visibility follows the cleanup checkbox.

The location-mode callback resolves selected storage immediately, keeping the explanation current before apply.
The deferred reader returns the selected record token at commit time.
After apply, update refreshes workspace applied state and the record-location explanation without changing workspace size.

Sidecar cleanup first asks Conduct whether the sidecar folder exists.
Absence announces that there is nothing to remove.
Presence captures the current attachment token in a confirmation continuation.
Acceptance while still attached clears records, refreshes workspace size and then announces the removed count in that order.

Detach removes cleanup, erase and slider handlers and disarms the captured pending-clear token.
It detaches workspace and renderer drivers before clearing the parent surface and answer.
A delayed answer invokes a no-op and cannot clear records or affect a later attachment.

## QSRenderer.cs

`QSRenderer` owns typed renderer path state independently of its attached controls and implements the production renderer port.
Every path change asks Conduct to classify program and library readiness.
The driver maps that state to one localization key and updates attached status.
Blank, ready, program-only, library-only and missing states each have a distinct key.

Detach removes text, browse, open and diagnosis handlers but retains typed path state, so reattach restores the latest value.
A cancelled folder dialog leaves the path unchanged.
Open and apply use the typed path when present.
The initial snapshot path is the fallback before any update.
The production port checks executable presence and library-folder validity separately.

## QSRendererAnswer.cs

The renderer answer boundary owns folder selection, folder opening and diagnosis presentation.
The production folder dialog starts at the current path and returns null on cancellation.

## QSWorkspace.cs

`QSWorkspace` owns workspace control handlers, dialog continuations and two destructive-maintenance flows.
Attach detaches the previous surface, creates fresh pending clear actions and subscribes controls.
It reads size and then refreshes applied and default state.
Only an applied workspace enables both clear commands.
An unapplied workspace shows its maintenance notice.

Browse uses the resolved default as its initial folder.
It accepts its continuation only while the same answer object remains attached.
Commit delegates collision policy to Conduct and restores the kept path after refusal.
It warns with the occupied destination.

Done cleanup first requires an applied folder, then confirmation.
Workspace reset asks Conduct whether the applied workspace is stopped.
Stopped work reaches confirmation, running work shows a warning, and an unapplied folder produces neither.
Accepted operations clear through Conduct, receive the post-clear size, update the size text and then announce the result.
Rejected confirmations never invoke their continuation.

Detach removes every control handler, replaces both pending destructive actions with no-ops and clears surface and answer references.
This makes confirmations completed after detach harmless and prevents an earlier attachment from mutating a replacement surface.

Sizes use bytes through terabytes, dividing by 1024 and showing at most two decimal places above bytes.

## QSWorkspaceAnswer.cs

The workspace answer boundary owns folder selection, confirmation, warning and announcement presentation.
Folder selection invokes its continuation only after acceptance.
Confirmation invokes its continuation only when the user accepts.

## Stamp

Commit: `9a34a722aa84`
- `QSFlyleaf.cs` `e2698b83737b76c4`
- `QSFlyleafAnswer.cs` `da8de300f659c5fd`
- `QSMpv.cs` `de18bb94be53f6df`
- `QSMpvAnswer.cs` `389550663c53a804`
- `QSOptionsOperation.cs` `f722589cbac476c5`
- `QSOptionsSystem.cs` `e09ec41e8c32d7cc`
- `QSRenderer.cs` `76ddc50788975175`
- `QSRendererAnswer.cs` `efc947956ecbf335`
- `QSWorkspace.cs` `bb6baa08096ae174`
- `QSWorkspaceAnswer.cs` `60e19d0543b78297`
