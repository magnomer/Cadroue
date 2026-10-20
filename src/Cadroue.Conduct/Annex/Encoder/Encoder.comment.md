# Encoder

This folder owns the UI-agnostic decisions for one encoder draft.

It converts capabilities and trial results into stable choices, normalizes port values, and leaves presentation to callers.

## CSEncoder.cs

`CSEncoder` holds the shared trial, repertoire, and draft-opening ports.

Each draft creation opens an encoder port for the preset and carries the caller's smart-cut permission into that draft.

A stored choice survives only when its rate exactly matches the active rate and the choice is not blank.

Rate resolution keeps an offered rate and otherwise uses the first codec mode.

Progress is complete when there is no work and otherwise equals `done / total`.

## CSEncoderDraft.cs

The draft assembles the video, trial, size, audio, suffix, location, and output gates around one port.

It attaches one help callback and raises `CSEncoderDraftReady` when that callback fires.

Apply trims a nonblank display name or substitutes `{OriginalName}_export`.

It forces the video stream to `Include` and writes the current video, size, and audio choices to the draft.

Closing detaches the callback, cancels both outstanding trials, and closes the port.

Video detail save enables only a `true` reactive state and translates the layout index to its stored token.

It trims quality and delegates the remaining values unchanged.

## CSEncoderVideo.cs

A video state resolves the requested rate against the selected codec before constructing quality, speed, notice, and extra rows.

Changing the encoder or resolved rate replaces the whole state and raises one event.

An equivalent choice changes nothing.

The stream modes are `Copy`, `Smart`, and `Encode`.

Invalid mode indices are ignored.

Smart notice selection reflects the permission captured for this draft.

Every non-smart mode uses the copied-stream notice.

Layout selection accepts only declared indices and otherwise stores an empty token.

The regular offer for a known container includes only compatible encoders that passed trial.

Unknown containers skip compatibility filtering.

A kept repertoire encoder that still fits may be inserted first even when unavailable.

Non-repertoire text is treated as available.

## CSEncoderCodec.cs

Codec rows expose quality and speed only when the corresponding capabilities exist.

Extras named by the selected mode's conflict list are omitted.

Extra resolution pairs supplied index positions with remaining extras in order and returns an ordinal-keyed option dictionary.

Supplying more indices than remaining extras throws.

## CSEncoderQuality.cs

Quality metadata is copied from the capability.

Stored quality is used only for the same active rate.

A differing rate or blank stored text falls back to the capability default.

## CSEncoderSpeed.cs

Speed labels and stored values preserve capability order, while slider positions reverse that order.

Stored speed is honored only for the active rate.

An absent value falls back to the capability default.

Out-of-range save indices resolve to an empty value.

## CSEncoderExtra.cs

Each extra preserves its option, label, default, and ordered value-label choices.

Its stored value is selected only while that value remains offered.

Otherwise resolution tries the capability default.

A nonempty choice list falls back to index zero when neither value resolves.

Invalid save indices produce an empty value.

## CSEncoderSize.cs

Sizes recognize positive dimensions separated by `x`, `X`, or `×`.

Exact standard dimensions, including swapped width and height, select the matching tier.

Other positive dimensions are custom, and invalid input becomes source size.

Tier and typed changes raise one event only when the resulting dimension changes.

A partially valid typed pair remains custom.

Persistence writes dimensions only for a non-source tier with two positive dimensions.

Knob values are capped at 7680 without changing the underlying dimension.

## CSEncoderFramerate.cs

The scale contains source at index zero and integer rates from 1 through 240.

It also includes 23.976, 29.97, 59.94, and 119.88 in sorted order.

Source-like, blank, non-finite, non-positive, and unparsable text resolves to index zero.

Other numeric text chooses the nearest offered rate.

An earlier sorted rate wins a tie.

Persistence maps source-like text to `Same as source`.

Display maps index zero to the caller's label.

Parsing and the offered numeric labels use invariant culture.

## CSEncoderAudio.cs

Audio choice changes reject empty encoder or rate text.

A kept change resolves the rate, replaces the whole state, and raises one event.

Opening combines that state with the draft stream, mode, and container-filtered encoder offer.

Audio detail read builds codec rows from the resolved choice.

It uses draft values unless the caller supplies a sample or channel override.

Capability-provided sample rates and layouts take precedence over the declared standard lists.

Invalid or non-positive sample text persists as `Same as source`.

A missing channel position also persists as `Same as source`.

Saving trims quality and stores empty speed or extras when no codec detail has been read.

Stream and mode are delegated unchanged.

Excluded stream or mode disables detail and selects the excluded notice.

Otherwise detail is enabled except for `Copy`, with the copied notice.

The regular offer for a known container includes only compatible and trial-available audio encoders.

Unknown containers skip compatibility filtering.

A compatible kept repertoire encoder may be inserted first even when unavailable.

Unknown encoder text is considered available.

## CSEncoderTrial.cs

Video trial runs every token of every repertoire entry.

An entry is available when any token succeeds.

The applied trial set contains every successful token using ordinal-insensitive identity.

Audio trial runs each candidate and applies the successful encoder names.

Both trial paths report progress after each attempt.

They retain result rows only after cancellation is checked and then record a summary with every attempt.

Starting a new run cancels the preceding run.

Cancellation propagates and prevents application of partial results.

The currently chosen repertoire entry is inserted first when none of its video tokens succeeded.

The chosen audio encoder is likewise inserted when it is a repertoire entry missing from successful results.

These insertions preserve a visible current choice without marking it available.

## CSEncoderSuffix.cs

Only `Rename existing` and `Rename output` have suffix fields.

Leaving a suffix-bearing policy trims and saves the shown suffix under the previous policy.

Normalizing a blank suffix clears its policy value and reads that value back from the port.

Location state distinguishes source, sibling, subfolder, and custom-location presentation.

Switching modes trims and saves the shown folder under the previous mode.

It then reads the new mode's folder.

The first selection has no previous mode to save.

## CSEncoderOutput.cs

Output read and extension lookup pass through the draft port.

Output save writes display, container, extension, collision, and location together.

It then trims and stores suffix by collision policy and folder by location policy.
