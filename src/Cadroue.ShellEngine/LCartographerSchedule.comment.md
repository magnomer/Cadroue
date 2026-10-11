# LCartographerSchedule.cs
Hash: `62e0d490b02830e0`

## `public static partial class LCartographer`

Watches the schedule, keeping source locks, batch eviction and delivery in step with it.

## `public static Func<IReadOnlyList<LWorkItem>, bool>? LCartographerLockSeam { get; set; }`

Claims the source files of work, and reports false when another owner already holds one.

## `public static void LCartographerStart()`

Subscribes only once, and only after a schedule contract is assigned.
Batches already scheduled are remembered first, so their later removal is noticed.
It then dispatches once, delivering work that finished before startup.

## `public static int LCartographerAccept(`

When a source lock is refused, the admitted items are withdrawn and recorded through LTraceEngine.
Plans created for a refused or empty admission are deleted, so no orphan plan files remain.
Accepted items then clear their sources from their source stage's pending inputs.

## `public static LCartographerPlanDocument? LCartographerPlanPrepare(Guid lCartographerTarget)`

Captures one plan before many items are routed, so every batch of a request copies the same route.
Returns null when there is nothing to route.

## `private static void LCartographerScheduleHandle(LScheduleContract lCartographerSchedule)`

Reads the live batches at notice time, then applies through LStation's post when one is assigned.
So relay changes run through the same post as station work.

## `private static void LCartographerSourcesRelease(LScheduleContract lCartographerSchedule)`

A source stays locked while active work in its batch still reads it.
Finished relayed work keeps its sources while its batch is still active.
An item whose sources were all unlocked is remembered, so it is not unlocked again.
The released set forgets items no longer scheduled.

## `private static void LCartographerBatchesUpdate(`

A batch that left the schedule is evicted from tabs and its plan file deleted.
Delivered marks for vanished work are dropped at the same time.
