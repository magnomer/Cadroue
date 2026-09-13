namespace Cadroue.ShellEngine;

public readonly record struct LSweepSpan(TimeSpan LSweepSpanOrigin, TimeSpan LSweepSpanEnd);

public readonly record struct LSweepBoundary(TimeSpan LSweepBoundaryTime, TimeSpan LSweepBoundaryMinimum);
