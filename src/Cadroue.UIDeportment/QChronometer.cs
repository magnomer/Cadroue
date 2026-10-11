using System.Diagnostics;

namespace Cadroue.UIDeportment;

public sealed class QChronometer
{
    private long qChronometerStamp;

    public void QChronometerStart() => qChronometerStamp = Stopwatch.GetTimestamp();

    public double QChronometerRead() => Stopwatch.GetElapsedTime(qChronometerStamp).TotalMilliseconds;
}
