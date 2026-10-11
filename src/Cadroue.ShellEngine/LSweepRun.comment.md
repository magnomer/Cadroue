# LSweepRun.cs
Hash: `23b778c7518478d2`

## `private static async Task<IReadOnlyList<string>> LSweepLinesRead(`

Every detection scan runs through here, so the analysis lane is claimed and released in one place.
The release sits in a finally, so a failed or cancelled run never leaves the lane held.
The detection run passes no work id, because a sweep never pauses with the queue.
Cancellation reaches the child through the employer, which kills the whole tree when the token fires.
So the sweep holds no process of its own and keeps no kill of its own.
A nonzero exit throws an invalid operation, which the fault check treats as a recoverable failure.

## `public static bool LSweepFaultCheck(Exception lSweepException) =>`

Names the failures a detection caller reports instead of crashing on.
They cover a missing program, an unreadable file and a failed ffmpeg run.

## `private static string LSweepFaultFormat(LEmployerResult lSweepResult)`

Only the last three error lines feed the autopsy, since ffmpeg puts the cause at the end.
A known cause shows its plain wording, an unknown one its technical text.
