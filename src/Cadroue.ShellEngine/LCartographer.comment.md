# LCartographer.cs
Hash: `79f1d08780ceff79`

## `public sealed record LCartographerDelivery(`

The tab-side callbacks delivery needs, assigned by the UI so the engine never names a tab.
Intake hands a file to a tab for the user, Track registers one an automatic stage will process.

## `public static partial class LCartographer`

Decides where each finished relayed output goes and drives that delivery.
Delivery state lives in static sets, so one dispatch pass serves the whole process.

## `public static Func<int, long, bool> LCartographerOwnerSeam { get; set; } = (_, _) => false;`

Answers whether the process and stamp of a work owner are still alive.
The host assigns the liveness registry, so the engine never names it.
Unattached, it reads false, so a foreign owner counts as gone and its planned work is delivered here.

## `public static readonly Guid LCartographerFinishTarget = new("feed0000-0000-0000-0000-0000000ffff0");`

A sentinel target meaning the relay chain ends here.
Work aimed at it is removed at its source and reaches no tab.

## `public static void LCartographerDeliveredRemove(IReadOnlySet<Guid> lCartographerLiveWork)`

Forgets the delivered and missing marks of work that left the schedule, so neither set outgrows it.

## `public static void LCartographerDispatch(IReadOnlyList<LWorkItem> lCartographerSchedule)`

A call made while a pass runs only flags another pass, so dispatch never re-enters itself.
An item is marked delivered before delivery, and the mark is undone when delivery fails or throws.
Each outer call ends with an LSeal run, so merge nodes see the deliveries just made.

## `public static bool LCartographerDeliverableCheck(LWorkItem lCartographerItem)`

Only finished work with a relay target, not yet delivered and not known missing, qualifies.
Work owned by another process qualifies only when a plan owns its stage and that owner is gone.

## `public static bool LCartographerItemDispatch(`

Without a delivery seam nothing is delivered, so the item stays eligible for a later pass.
A missing output file is remembered, so the same item is not retried every pass.
The source is dropped only when no other undelivered work in the batch shares a source path.

## `private static bool LCartographerStageDispatch(`

The plan records each delivered work id and is saved, so a restart never delivers it twice.
