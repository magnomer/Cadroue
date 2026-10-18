# QRosterDetail.cs
Hash: `03f8da7c76925d17`

## `public static QRosterDetailKind QRosterKindRead(QRoster lRoster)`

A selected card title wins over a selected job, so the pane shows the batch summary first.

## `public static QRosterDetail QRosterDetailRead(QRoster lRoster)`

Builds the detail of the roster's current step, or the empty detail when none is shown.

## `public static QRosterDetail QRosterDetailCreate(LWorkItem? lWorkItem, string lOwnerText)`

Builds the whole job detail as plain records so the veneer draws without deciding.
With no job it returns every list empty, so the veneer needs no null branch.
The owner text comes in as an argument because owner formatting belongs to QRoster.
The meter line appears only when a size bar exists and the job has spent time.
A failed job shows its message as the failure row, any other job as a record row.

## `public static string? LRosterDetailOpen(string lPath) => LUsher.LUsherPathOpen(lPath, "Roster file");`

Returns the shell error so the veneer can announce it.
`LUsher` records the failure under the `Roster file` label.

## `public static IReadOnlyList<QRosterBar> QRosterBarsCreate(long? lSourceBytes, long? lOutputBytes)`

The bar has a rest part and a mark part whose meaning flips when the output outgrows the source.
Both parts keep a tiny floor so neither is ever exactly zero.
Unknown or zero source sizes yield no bar, since a ratio would be meaningless.

## `private static IReadOnlyList<QRosterCompareRow> QRosterComparesCreate(`

The first row is the Source and Output header pair.
The output side prefers the measured output media and falls back to the requested encoding.

## `private static IReadOnlyList<QRosterCompareRow> QRosterSoundsCreate(LWorkItem lWorkItem)`

Returns no rows when neither side has audio, which hides the sound section.
Without an active audio step the output loudness repeats the source, since the stream is untouched.

## `public static QRosterCompareRow QRosterCompareCreate(string lSource, string lOutput)`

The output line is keyed Changed whenever its text differs from the source, so the veneer highlights it.

## `private static IReadOnlyList<QRosterDetailRow> QRosterVideosCreate(LEncodingVideo lVideo)`

Encoder rows follow only for a re-encode, since a stream copy has no encoder settings.

## `private static IReadOnlyList<QRosterDetailRow> QRosterAudiosCreate(LEncodingAudio lAudio)`

Encoder, sample rate and channel rows follow only for a re-encode.
The speed row appears only when a speed value is set.
