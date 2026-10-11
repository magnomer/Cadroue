# TMapBands.cs
Hash: `a3ec667a5266ae20`

## `public sealed class TMapBands`

Checks the band and badge records the map deportment resolves for the veneer to draw.
The flow holds a 60 second source with two sections, 10 to 30 seconds and 30 seconds to the end.
Positions are measured on a 600 pixel strip.

## `public void Bands_FollowSections_WithBadge_Selection_AndHidden()`

Each section becomes one band positioned by its time range.
A band carries its badge number, its selected flag and its shown flag.
Hiding a section hides its band and records the sections draw trigger.

## `public void Badge_CentresInTheBand_AndSkipsWhenItDoesNotFit()`

The badge is centred in its band.
A measured text that does not fit inside the band with its margin yields no badge.
