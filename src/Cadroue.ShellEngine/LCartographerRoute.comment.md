# LCartographerRoute.cs
Hash: `2500a6ccf9728e9c`

## `public static partial class LCartographer`

Holds each tab's chosen relay target and resolves where a relayed file goes next.

## `public static void LCartographerTargetSet(Guid lCartographerSourceTab, Guid lCartographerTarget)`

An empty target clears the route.
A tab relaying into itself is refused and recorded through LTraceEngine.

## `public static void LCartographerTabRemove(Guid lCartographerTabId)`

Also drops every route aimed at the removed tab, so no source keeps a dangling target.

## `public static Guid LCartographerRouteRead(LCartographerStageEntry lCartographerStage, string lCartographerPath)`

Matches on the file name only, and only rules with a target stage are eligible.
Returns an empty id when no rule routes the file.

## `public static IReadOnlyList<Guid> LCartographerRelaySet(`

A batch that already has a plan keeps it, and its items are re-aimed at stable stage ids.
A new batch gets a fresh or copied plan, unless the target is empty or the finish target.
When no plan can be built or saved, items fall back to the raw tab target.
Returns only the plans created here, so the caller can delete them if admission fails.

## `public static bool LCartographerCycleCheck(`

Follows filename rules through automatic funnel tabs and reports whether the route revisits a tab.
The walk stops at the first tab that is not an automatic funnel.
