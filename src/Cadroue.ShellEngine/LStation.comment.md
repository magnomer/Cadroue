# LStation.cs

## `public static bool LStationActiveCheck()`

Any post currently processing.
Background source measurement consults this.
So its whole-file disk reads never run alongside a job's reads on the same spinning disk.
