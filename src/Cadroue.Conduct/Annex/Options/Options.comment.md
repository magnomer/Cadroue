# Options

This folder owns Options decisions while UI stays with callers and external operations stay behind collaborators.

## CSOptions.cs

`CSOptions` holds the supplied preference draft, a staged `LOptionsSnapshot` and separate read and apply request functions.
Construction materializes the first snapshot from the draft.
Later snapshot refreshes preserve staged General values while rereading the remaining values from the draft.
Snapshot reads copy the selection into a read-only array so callers cannot mutate staged selection through the returned list.

General changes stage in the snapshot through the `CSOptionsUpdate` overloads.
Playback, Timeline, Work and System sheets commit directly to the draft through `CSOptionsSheetCommit`.
Applying refreshes the snapshot and copies every snapshot value and custody level into the draft.
It saves a clone and sends a renderer apply request with the selected engine.
The renderer request still runs when saving returns false, and the result reports only the save result.

Workspace resolution resolves the requested and current folders before checking occupancy.
When both resolved paths match without case, resolution skips occupancy.
An occupied alternative keeps the current folder.
Otherwise resolution keeps the requested text.
Workspace commit writes that kept value to the draft.

The applied check resolves a folder and compares it without case with the root returned by the read request.
The running check invokes neither callback for an unapplied folder.
For an applied folder, it reports either running or the resolved root as stopped.
Default resolution substitutes the default returned by the read request only for blank text.
Folder opening delegates to `CUsher`, using either an explicit fallback or the resolved default.

Done and workspace clears apply their clear request before reading and returning the remaining size.
Sidecar clear returns the apply fact count.
Record mode recognizes only the exact token `Workspace`.
Changing it returns the corresponding localization key and the current sidecar location.

Engine selection returns `Mpv` only when the caller enables it and the staged engine is exactly `Mpv`.
Every other case returns `Flyleaf`.
Language resolution accepts an exact configured key, then falls back to the first configured key, then to an empty string.
Restart detection compares normalized stored language with the active localization language without case.

## CSFlyleaf.cs

`CSFlyleaf` rejects a null port and delegates the installed check.
Each instance admits only one active start, so a rejected overlapping request neither projects a start nor calls the port.
The gate is released in `finally` after the accepted port call and before its outcome callback.
A successful port tuple routes only to `completed`.
Failure routes only to `failed` with the port's installed state and message.

## CSFlyleafPort.cs

The port separates the synchronous installed fact from an asynchronous start operation with progress reporting.

## CSMpv.cs

`CSMpv` rejects a null port and delegates installed and availability checks separately.
Each instance admits only one active start, so a rejected overlapping request neither projects a start nor calls the port.
The gate is released in `finally` after the accepted port call and before its outcome callback.
Start then awaits exactly one asynchronous completion or failure callback selected by the success flag.

## CSMpvPort.cs

The port keeps installed state, runtime availability and the asynchronous start operation separate.

## CSRenderer.cs

`CSRenderer` rejects a null port.
A blank path produces `CSRendererStateBlank` without probing the port.
For nonblank paths it always checks both program and library.
Both present is ready.
Program alone or library alone has its matching partial state.
Neither present is missing.
The returned snapshot retains the supplied path.

## CSRendererPort.cs

The renderer port exposes independent program and library checks for a candidate folder.

## Stamp

Commit: `9a34a722aa84`
- `CSFlyleaf.cs` `eee496e7818e525e`
- `CSFlyleafPort.cs` `c73803637dfa35df`
- `CSMpv.cs` `5a6f606e4d7c4385`
- `CSMpvPort.cs` `8dd65242d53748a4`
- `CSOptions.cs` `405f2006e426ca08`
- `CSRenderer.cs` `87f6d92c287b0711`
- `CSRendererPort.cs` `1b1e5ce2face5c50`
