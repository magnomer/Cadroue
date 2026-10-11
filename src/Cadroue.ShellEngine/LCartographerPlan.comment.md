# LCartographerPlan.cs
Hash: `29a86380d824d9ac`

## `public static partial class LCartographer`

Captures the relay route as a plan of stable stages, so later tab edits cannot reroute queued work.

## `public static Func<IReadOnlyList<LCartographerTab>>? LCartographerTabsSource { get; set; }`

The UI supplies the live tab list, since the engine never reads tabs itself.
Left unassigned, no plan can be captured.

## `public static LCartographerPlanDocument? LCartographerPlanCreate(Guid lCartographerPlanId, Guid lCartographerTarget)`

Each tab becomes at most one stage, so tabs reached by several routes share it.
A route back into a stage still being built ends at the finish target instead of looping.
Funnel tabs keep one stage target per rule, other tabs follow their single relay target.
Returns null when no tabs are known or the entry tab no longer exists.

## `public static LCartographerPlanDocument LCartographerPlanCopy(`

A JSON round trip gives each batch an independent deep copy of a prepared plan.
The copy starts with no delivered work and no pending inputs, so no state leaks between batches.
