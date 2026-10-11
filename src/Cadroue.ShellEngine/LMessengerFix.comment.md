# LMessengerFix.cs
Hash: `2cec71890a28a173`

## `public static partial class LMessenger`

Turns a Fix tab request into work items and routes them into the schedule.

## `public static int LMessengerFixDescribe(`

Without an export encoding nothing is queued and the count is zero.
Each source's saved Fix plan is read and handed to the planner, keyed by path.
Each source keeps the batch it arrived with.
Sources that arrived without one share a single batch minted for this request.
