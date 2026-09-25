# ffmpeg-error-spine.json

The table `LAutopsySpine` looks an FFmpeg exit code up in.

## `normalize`

The rule that folds a raw exit code into a signed 32-bit value before the lookup.

## `errors`

One entry per normalized code, with its symbol, category, severity and retry advice.
The localized texts for each code live in `localization/FFmpeg-error`.
