# PRosterOverview.cs

## Inline notes

### `string pSourceUnknown = LLocalization.LLocalizationTextRead(`

Any source figure still absent is "Measuring" until measurement has actually been attempted for this item.
Only then does a missing figure mean "Unknown" (unreadable).
The flag decides, not the presence of a partial probe.
So a figure the deferred whole-file measurement has not filled yet never reads as "Unknown".
