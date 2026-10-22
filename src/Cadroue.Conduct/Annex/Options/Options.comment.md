# Options

This folder holds the policy of the Options window.
It decides and leaves every control and dialog to Deportment.
Outside facts reach it through two request functions and three port interfaces, so tests pass fakes.

## CSOptions.cs

`CSOptions` holds the draft preference and a staged snapshot of the General fields.
It reaches the depot's folders, sizes and clears only through the read and apply request functions.
  The save goes through `LPreference`, and folder opening through the usher.

The General fields stay in the snapshot until apply copies them into the draft.
Every other field is read from the draft on each refresh, so an edit made after construction is not lost.
Test `RecordCheck_PreservesPendingGeneralEdits` pins the General half.
Test `Apply_PreservesCompatibilityDraftEditsAfterConstruction` pins the draft half.

Apply saves a clone of the draft, then sends the renderer request that switches the engine.
The engine switch runs even when the save failed.
Test `Apply_PreservesPersistenceRendererCallbackOrder` pins the order and the failed-save case.

Resolving a workspace folder sends two resolution requests, then one occupancy request.
An occupied destination keeps the current folder and reports the destination through the callback.
A destination that resolves to the current folder skips the occupancy request.
Test `WorkspaceResolve_KeepsCurrentWhenDestinationOccupied` pins the request order and the kept folder.

The commit writes the kept folder into the draft.

The workspace resolve and the applied, default and running checks report through callbacks.
A driver therefore cannot fold two answers into one.
The applied check resolves the folder and compares it with the depot root.
The running check does the applied check first, and an unapplied folder answers neither callback.
The default check reports the default case when the text is blank.
Only a blank folder resolves to something else.

The done and workspace clears send the clear request, then a second request that reads the size.
The returned count and size therefore describe the state after the clear.
Test `DoneClear_Accepted_PreservesEffectOrder` pins that the size shown at the announcement is the cleared size.

The engine read returns Mpv only when the caller says mpv is usable and the snapshot asks for it.
Test `Engine_FallsBackToFlyleaf_WithoutMpv` pins the fallback.

## CSFlyleaf.cs

The gate forwards the installed check and the install to its port.
The install answers a tuple of success, message and installed state.

## CSMpv.cs

The gate adds an availability check beside the installed check and the install.
The availability check is a separate port call, so installed and usable stay apart.

## CSRenderer.cs

A blank path reports the blank state without asking the port.
Otherwise the program and library facts map to ready, program only, library only or missing.
Test `CSRendererRead` pins the blank state and the four fact mappings.

## Port files

Only the renderer port file declares its fact type beside its interface.
The Deportment driver implements each port with the real library, and tests pass fakes.

## Stamp

Commit: `b84e5458de50`
- `CSFlyleaf.cs` `cedcd83201f1480f`
- `CSFlyleafPort.cs` `c73803637dfa35df`
- `CSMpv.cs` `b88e1a034631c511`
- `CSMpvPort.cs` `8dd65242d53748a4`
- `CSOptions.cs` `6a7d54810955b46c`
- `CSRenderer.cs` `735d02a075387767`
- `CSRendererPort.cs` `fda5b123de977a35`
