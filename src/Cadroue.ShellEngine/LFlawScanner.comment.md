# LFlawScanner.cs
Hash: `5eacd1719489e567`

## `public static async Task<IReadOnlyList<LDossier>> LFlawScanRun(`

Public so the host and the test relay can hand it to the Infrastructure checkup as its scanner seam.
The engine thus never names the checkup, while the checkup still runs the engine scan.
An empty kind set returns every detected defect, otherwise only the listed kinds survive.
Probes and processes go through the engine seams, so the engine never references Media or Infrastructure.

## `internal static void LFlawProgressApply(`

Maps one ffmpeg progress line onto the current stage's share of the whole scan.
Only ffmpeg stages report progress, and only when the source duration is known.

## Inline notes

### `if (string.IsNullOrWhiteSpace(lFlawSource) || !File.Exists(lFlawSource))`

A source that is missing or unreadable cannot be diagnosed.
Returning an empty (defect-free) result here would be recorded by callers as an authoritative "clean" verdict for every kind.
That false negative then suppresses any real scan.
Fail instead so no diagnosis record is written for a scan that never happened.

### `catch (Exception lFlawDurationException) when (`

An unreadable duration only disables fine progress, it never aborts the scan.

### `(_, string lFlawTransportError) = await LFlawStageRun(`

Transport-stream continuity and PES faults are logged at warning level, not error.
So the transport probe reads one verbosity higher than the shared copy pass.
It feeds only the MPEG-TS-gated transport detector, never the others.

### `(_, string lFlawSeekError) = await LFlawStageRun(`

Seek to one second before end: a late target forces the demuxer to consult the index.
So broken random-access addressing surfaces here while a healthy file stays silent.
A near-start seek (a large -sseof on a short clip) would read linearly and never touch the index.

### `bool lFlawOpened = lFlawMetaReport.Contains("[FORMAT]", StringComparison.Ordinal)`

A container the probe could open reports at least a format or one stream.
When it reports neither, the file never opened.
Every structural and per-stream probe below only echoes that one open failure.
Emitting the finalization defect alone keeps the diagnosis honest.
It avoids scattering the same failure across the container, coded and timing detectors.

### `if (!lFlawDossiers.Any(lFlawDossier => LFlawCarriageCheck(lFlawDossier.LDossierKind))`

Coded media is the last-resort, lossy re-encode item.
The diagnostic decode also fails whenever an upstream carriage defect corrupts the bitstream it reads.
Such defects are a broken container, missing finalization, transport faults, framing, codec configuration or an FFV1 integrity mismatch.
That decode failure is already explained by a losslessly repairable defect.
So it must not escalate this file to re-encode.
Only decode damage that survives every carriage diagnosis is a genuine coded defect.

### `catch (Exception lFlawException)`

A scan that could not complete must not be mistaken for a clean file.
Returning an empty result would let callers persist a false "no defect" record.
That record then blocks any future diagnosis of every kind.
Surface the failure so the caller reports it and writes nothing.
