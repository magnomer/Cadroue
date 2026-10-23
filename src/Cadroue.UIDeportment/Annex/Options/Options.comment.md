# Options

These drivers project Conduct-owned Options state onto passive Veneer surfaces.
They own the event handlers they add and expose control values to the composition root.
They decide neither persistence nor platform behavior.

## QSOptions.cs

`QSOptions` composes one `CSOptions` draft owner with General, System, Playback, Timeline and Work drivers.
It also composes workspace, renderer and player owners.
The production constructor supplies infrastructure requests and current mpv availability.
The alternate constructor accepts player ports for controlled composition.

Commit reads Playback, Timeline, Work and System controls into the Conduct draft, then commits the workspace driver.
Apply reads General last and asks Conduct to save, returning only the saved flag.
Detach releases the four sheet drivers that own handlers in this folder or the System subfolder.

Page selection retains any enum value at or above `QSOptionsPageGeneral`.
Spectrum creation seeds a new spectrum from the current snapshot and delegates palette reads to Conduct.
Priority names and title are localized when read.

## QSGeneral.cs

`QSGeneral` can operate as a surface-only projection or forward changes to `CSOptions` when supplied an owner.
It copies selection when storing a snapshot.
Attach detaches any previous surface and localizes the General labels.
It builds startup and orientation radio segments, then initializes the picker, checkboxes and language list.
Finally, it subscribes its radio handlers.

Startup and orientation radio changes update the snapshot immediately and, when present, their matching Conduct fields.
The tab picker is visible only for `DefaultTab`.
Selection, recent, safety, vacancy and language are sampled together by `QSGeneralSnapshotRead` and then forwarded to Conduct.
Detach samples those controls before removing radio handlers, preserving edits for a later attachment.

Unknown startup or orientation tokens fall back to the first declared choice.
Language selection falls back to an empty string when the configured key is absent.

## QSOptionsAnswer.cs

`QSOptionsAnswer` is the window's boundary for surface and shell creation, tab styling, headers, placement, notices, confirmation, diagnosis and choice widgets.
It stores the supplied delegates and performs no policy of its own.

## QSOptionsField.cs

`QSOptionsField` clamps initial slider values to the control's bounds and formats readouts as rounded whole numbers.
It records each value-change handler it adds.
Detach removes every recorded slider handler and clears the registry.
Checkbox preparation sets content and state before applying interaction styles.
A checkbox reads true only when `IsChecked` is true.

## QSOptionsPlayback.cs

Playback attach begins by detaching its mpv and slider subscriptions, then projects the current snapshot.
The engine selector asks Conduct for a choice using current mpv availability.
It disables mpv when unavailable and follows later `QSMpvChanged` notifications.
The deferred reader returns autoplay, volume scope and value, wheel action, drag behavior and the currently selected engine.
Detach removes the mpv subscription and all slider readout handlers.

## QSOptionsTimeline.cs

Timeline attach first detaches old slider handlers.
It then projects ordering, keyframe distance, delay, overlap, waveform and the supplied palette surface.
Its deferred reader samples the controls and calls the supplied spectrum reader at commit time.
Detach removes the slider readout handlers.

## QSOptionsWindow.cs

The window composes the passive surface, tab headers, placement owner and answer adapters before showing it modally.
Attach creates the spectrum dialog and attaches every sheet and player surface.
It styles the three command buttons.
It subscribes commands and tab selection.

Apply commits all sheet values before saving.
It invokes the optional applied callback and updates the System display even when saving fails.
A failed save shows the failure warning and suppresses the restart notice.
A successful save shows the notice only when Conduct reports a required language restart.
OK applies before closing, while Cancel closes without applying.

Close detaches the spectrum dialog, workspace, sheet, renderer and player owners.
It saves placement and releases the window grabber.
It then removes button, selection and close handlers.

## QSOptionsWork.cs

Work attach detaches the prior surface, projects failure behavior and three custody priorities, then subscribes retry and priority handlers.
Retry controls whether the retry-limit element is enabled.
Priority changes are forwarded immediately to Conduct.
Pause, retry and maximum are sampled by the deferred reader during commit.
Detach removes every handler added by this driver, detaches slider readouts and drops the surface reference.

## Stamp

Commit: `9a34a722aa84`
- `QSGeneral.cs` `56cfac258d605250`
- `QSOptions.cs` `6349e711d5111877`
- `QSOptionsAnswer.cs` `da99e3a56383bff7`
- `QSOptionsField.cs` `7832cd7b1029fde7`
- `QSOptionsPlayback.cs` `5a8a6a483033a50c`
- `QSOptionsTimeline.cs` `c0663331985691b7`
- `QSOptionsWindow.cs` `46a9264751175ee1`
- `QSOptionsWork.cs` `d6bbead423d43e67`
