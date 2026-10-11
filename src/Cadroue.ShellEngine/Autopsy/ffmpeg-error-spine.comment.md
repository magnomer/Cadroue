# ffmpeg-error-spine.json
Hash: `b43078a6c4011aa6`

The table `LAutopsySpine` looks an FFmpeg exit code up in.
It ships as an embedded resource, so a missing table fails loudly at first use.

## `normalize`

The rule that folds a raw exit code into a signed 32-bit value before the lookup.
`LAutopsy` applies the same fold in code, and the string here is never read.

## `errors`

One entry per normalized code, with its symbol, category, severity, visibility and retry advice.
The localized texts for each code live in `localization/FFmpeg-error`.
