# LFlawFfvone.cs

## `public const string LFlawReport = "report"`

Detection-only repair marker.
An FFV1 slice-CRC mismatch proves the covered slice is inconsistent, not which byte changed.
ffmpeg cannot correct it.
The dossier reports the damage and never claims the original bytes were restored.
The Fix pipeline treats a report-only dossier as an unresolvable defect.
The file is copied unchanged and the item ends Unresolved.

## Inline notes

### `if (!LFlawFfvoneCheck(lFlawProbeReport))`

Applies only to FFV1 streams carrying slice CRCs.
Any other codec is not applicable and raises no dossier.
