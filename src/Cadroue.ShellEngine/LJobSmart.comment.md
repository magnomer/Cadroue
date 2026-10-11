# LJobSmart.cs
Hash: `b287e4f1ef9251a6`

## `private (int, string) LJobLeadingRun(LEncodeStage pStage)`

The splice stage rewrites the copied middle in place, without spawning ffmpeg.
A file that needs no change is left untouched.

## `private async Task<(int, string)> LJobSmartRun()`

Smart-mode jobs rejected earlier by eligibility checks use the ordinary stage path instead.
A Whole or Invalid plan falls back to one whole-interval encode, not only a missing copyable middle.
The media probe for the open-end decision receives the runner's priority override.
