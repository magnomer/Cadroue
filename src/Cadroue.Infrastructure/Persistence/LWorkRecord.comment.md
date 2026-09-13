# LWorkRecord.cs

## `public bool? LWorkSourceMeasured { get; set; }`

Nullable so a record written before this field existed (null) is treated as already measured.
A fresh item explicitly persists false while its measurement is still pending.
