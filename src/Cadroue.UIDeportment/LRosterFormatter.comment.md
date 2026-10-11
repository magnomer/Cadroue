# LRosterFormatter.cs
Hash: `6ec01a0a6f7c1c15`

## `public static bool LRosterReencodeCheck(string lMode) =>`

Any mode other than `Copy` or `Exclude` re-encodes the stream.

## `public static string LRosterUnknownFormat(LWorkItem lWorkItem)`

Any source figure still absent is "Measuring" until measurement has actually been attempted for this item.
Only then does a missing figure mean "Unknown" (unreadable).
The flag decides, not the presence of a partial probe.
So a figure the deferred whole-file measurement has not filled yet never reads as "Unknown".

## `public static string LRosterElapsedFormat(TimeSpan lSpent)`

Rounds up to whole seconds with one second as the floor.
A job that has started therefore never reads as zero time.

## `public static string LRosterMeterFormat(string lSpent, string lSpeed)`

Joins a spent time and a speed into the one meter line.
The job detail and the batch summary both show it, so one owner keeps the text identical.

## `public static TimeSpan? LRosterSpentRead(LWorkItem lWorkItem)`

A running job is measured up to now.
A finish before the start, from a changed clock, reads as no time rather than a negative one.

## `public static string LRosterCodecFormat(LWorkMedia? lMediaInfo) =>`

The audio figures show only for media with a sample rate, since only then is an audio stream present.
