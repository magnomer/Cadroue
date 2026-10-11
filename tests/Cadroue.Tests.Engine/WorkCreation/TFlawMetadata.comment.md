# TFlawMetadata.cs
Hash: `4550e9e21d4208a8`

## `public void DisagreeingTrackTimelines_ProduceMetadataDossier()`

Video declares a 20s timeline over the same essence the 4s audio track spans.
The format duration agrees with the longest track.
The inflated per-track timescale is still a metadata defect.

## `public void CloseTrackTimelines_ProduceNoMetadataDossier()`

Audio a hair longer than video is normal interleaving, not a defect.
