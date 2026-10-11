# LCartographerPlanStore.cs
Hash: `df25837dc9b6b971`

## `public static class LCartographerPlanStore`

Persists one relay plan per batch as a JSON file under the depot root.
The root comes through LDepotEngine, so the store never names the Infrastructure depot.
Every parse, write and delete holds an LLatch on the plan path, so processes never interleave them.
Expected file failures are recorded through LTraceEngine instead of thrown.

## `private sealed record LCartographerPlanEntry(`

A parsed plan is reused while its file keeps the same write time and length.
A change by another process alters the stamp or length, which forces a fresh parse.

## `public static bool LCartographerPlanSave(LCartographerPlanDocument lCartographerPlan)`

Writes a unique temporary file, then moves it over the plan, so readers never see half a plan.
A failed save drops the cached copy, so the next read goes back to disk.

## `private static bool LCartographerTextParse(`

Fills absent collections with empty ones, so a plan file missing a field reads without null checks downstream.
