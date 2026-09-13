# TBridgeLeading.cs

## `private static byte[] TBridgeMdatCreate(params byte[] nalBytes)`

Builds a minimal ISO-BMFF file: one mdat box holding length-prefixed NAL units, each given here as a (header, second) pair.
