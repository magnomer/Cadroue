# LMediaTrace.cs
Hash: `1796c3f24731a011`

## `public static class LMediaTrace`

Owns the trace text of a media-open outcome.
It holds no loader state, so the viewer calls it after each open settles.

## `public static Action<string, Exception?>? LMediaErrorSeam { get; set; }`

Carries rejection and fallback records to the trace, which this layer cannot reach.
The loader reports a failed load-notice handler through it too.
The host fills it at startup.

## `public static Action<string>? LMediaOpenSeam { get; set; }`

Carries the info record of a successful open to the trace.
The host fills it at startup.

## `public static void LMediaOpenRecord(LCargo lCargo, bool lPlayerReady)`

Records the Flyleaf open outcome: the rejection, or the opened streams and a missing preview.
The player readiness comes from the caller, since only the viewer knows whether its player started.

## `public static void LMediaMpvRecord(LCargo lCargo)`

Records the mpv open outcome, which carries the duration but no stream summary.
Media without a probe result is recorded as rejected.

## `public static void LMediaRebuildRecord(string lSourcePath, string lMpvReason, bool lEditEligible)`

Records why mpv could not open the file and what the viewer does next.
The Edit tab has no Flyleaf fallback, so its message says the preview is unavailable.
