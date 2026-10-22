# LFlawConfig.cs

## Inline notes

### `string lFlawFilter = lFlawExtradata ? "dump_extra" : "extract_extradata"`

Without out-of-band extradata, the configuration is derived from the in-band headers.
That changes only container-side configuration and leaves the packets exact.
With extradata present but inconsistent with the samples, the stored configuration is reinserted into the packets.
That is a change in the coded carriage and leaves the coded units unchanged.
An unknown parameter set is never synthesized, since that would cause a silent misdecode.
