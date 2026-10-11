# LMessengerSplit.cs
Hash: `0545bbe4f206a70c`

## `public static partial class LMessenger`

Turns a Split tab request into work items and routes them into the schedule.
The single-source form prepares one relay plan, so every section item follows the same route.
The list form reads each source's saved sections away from the calling thread.
It then queues through LStation's post when one is assigned.
So its returned count covers only work queued before the call returns.
List failures are recorded through LTraceEngine and reported as zero queued.

## `private static async Task<IReadOnlyList<LSplitPlanDescription>> LMessengerSplitCreate(`

Sources with no valid sections are left out, so they queue nothing.

## `internal static async Task<IReadOnlyList<LSplitSectionDescription>> LMessengerSplitRead(`

A source without a sidecar yields no sections.
An unknown duration leaves sections bounded only by their own ends.
Invalid sections are dropped, and how many is recorded through LTraceEngine.
Any failure yields no sections instead of throwing.
