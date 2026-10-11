# TInterfaceRoster.cs
Hash: `2b6e0aae12d29aa4`

## `internal static class TInterfaceRoster`

The roster area of the test interface.
Tests reach the station, roster, lineage and summary deportments only through these forwards.
It is a separate class, not a partial part, so its size stays visible.
Most forwards add no logic.

## `internal static LStation TStationCreate(LScheduleContract schedule)`

Builds a station on the supplied schedule with no real runner environment.
Posted actions run inline, so a test sees every effect before its next line.
