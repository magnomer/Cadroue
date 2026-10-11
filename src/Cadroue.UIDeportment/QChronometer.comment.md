# QChronometer.cs
Hash: `d5120ccc29e90dd8`

## `public sealed class QChronometer`

Times one GUI step at a time, such as a strip draw or a player creation.
Each owner holds its own instance, so the stamp never crosses owners.

## `public void QChronometerStart() => qChronometerStamp = Stopwatch.GetTimestamp();`

Stamps the start of the step and replaces any earlier stamp.

## `public double QChronometerRead() => Stopwatch.GetElapsedTime(qChronometerStamp).TotalMilliseconds;`

Returns the milliseconds elapsed since the last start.
