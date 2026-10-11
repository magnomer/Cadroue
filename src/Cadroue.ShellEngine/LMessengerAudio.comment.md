# LMessengerAudio.cs
Hash: `a37db44386d79a87`

## `public static partial class LMessenger`

Turns an Audio tab request into work items and routes them into the schedule.
Without an export encoding nothing is queued and the count is zero.
The single-source form uses the plan the tab supplies, under the batch the caller minted.
The list form reads each source's saved audio plan and skips sources whose plan is inactive.
Sources without a batch of their own share one batch minted for this request.
Created items then get their source sizes and a deferred LSubsidiary measurement.
