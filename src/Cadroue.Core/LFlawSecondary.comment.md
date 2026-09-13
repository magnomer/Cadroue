# LFlawSecondary.cs

## `private static readonly string[] lFlawSecondaryBenign`

A secondary-only copy over an input carrying no subtitle/data output stream makes the null muxer complain.
That is not a defect in a secondary object.

## Inline notes

### `if (lFlawSecondaryStreams.Count == 0 && lFlawChapters.Count == 0)`

Principal A/V never reaches this diagnosis.
With no secondary stream and no chapters there is no secondary object to be malformed.
