# TInterfacePlayer.cs
Hash: `6b038151bca35249`

## `internal static class TInterfacePlayer`

The player area of the test interface.
Tests reach the player and its engine seam only through these forwards.
It is a separate class, not a partial part, so its size stays visible.
Most forwards add no logic.

## `internal static QPlayerSeam TPlayerSeamCreate(`

Builds a player seam whose unset actions do nothing and whose unset reads return zero or false.
Its facts member ignores the facts, so a test player records no renderer lines.
