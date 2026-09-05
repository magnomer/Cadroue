using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cadroue.Infrastructure;

public static partial class LTrace
{
    private sealed class LTraceDrawTally
    {
        private readonly Dictionary<string, int> lTraceTriggerCounts = new(StringComparer.Ordinal);

        private int lTraceRenderCount;
        private double lTraceTotalMilliseconds;
        private double lTracePeakMilliseconds;
        private long lTraceGlyphTotal;

        internal void LTraceDrawAdd(string lTraceTrigger, double lTraceMilliseconds, int lTraceGlyphCount)
        {
            lTraceRenderCount++;
            lTraceTotalMilliseconds += lTraceMilliseconds;
            lTraceGlyphTotal += lTraceGlyphCount;
            if (lTraceMilliseconds > lTracePeakMilliseconds)
            {
                lTracePeakMilliseconds = lTraceMilliseconds;
            }

            lTraceTriggerCounts.TryGetValue(lTraceTrigger, out int lTraceSeen);
            lTraceTriggerCounts[lTraceTrigger] = lTraceSeen + 1;
        }

        internal string LTraceSummaryRead(string lTraceSurface) => string.Create(
            CultureInfo.InvariantCulture,
            $"{lTraceSurface} drew {lTraceRenderCount}x in the last second");

        internal string LTraceDetailRead()
        {
            var lTraceBuilder = new StringBuilder();
            double lTraceAverage = lTraceRenderCount == 0 ? 0 : lTraceTotalMilliseconds / lTraceRenderCount;
            lTraceBuilder.Append(CultureInfo.InvariantCulture,
                $"avg {lTraceAverage:0.00}ms, peak {lTracePeakMilliseconds:0.00}ms, total {lTraceTotalMilliseconds:0.0}ms");

            if (lTraceGlyphTotal > 0)
            {
                double lTracePerRender = lTraceRenderCount == 0 ? 0 : (double)lTraceGlyphTotal / lTraceRenderCount;
                lTraceBuilder.Append('\n');
                lTraceBuilder.Append(CultureInfo.InvariantCulture,
                    $"{lTraceGlyphTotal} FormattedText built ({lTracePerRender:0.#}/draw)");
            }

            if (lTraceTriggerCounts.Count > 0)
            {
                lTraceBuilder.Append('\n');
                lTraceBuilder.Append("triggers: ");
                bool lTraceFirst = true;
                foreach (KeyValuePair<string, int> lTraceTrigger in lTraceTriggerCounts)
                {
                    if (!lTraceFirst)
                    {
                        lTraceBuilder.Append(", ");
                    }

                    lTraceBuilder.Append(CultureInfo.InvariantCulture, $"{lTraceTrigger.Key} {lTraceTrigger.Value}");
                    lTraceFirst = false;
                }
            }

            return lTraceBuilder.ToString();
        }

        internal string LTimelineDetailRead(string lTraceSurface)
        {
            string lTraceDrawLabel = lTraceRenderCount == 1 ? "redraw" : "redraws";
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{lTraceSurface}: {lTraceRenderCount} {lTraceDrawLabel}\n  {LTraceDetailRead().Replace("\n", "\n  ", StringComparison.Ordinal)}");
        }
    }

    private sealed class LTraceTimelineTally
    {
        private readonly Dictionary<string, LTraceDrawTally> lTraceSurfaceTable = new(StringComparer.Ordinal);
        private readonly List<string> lTraceSurfaceOrder = [];
        private TimeSpan lTraceCursor;
        private string? lTraceSourcePath;

        internal void LTraceDrawAdd(
            string lTraceSurface,
            TimeSpan lTraceDrawCursor,
            string? lTraceDrawSourcePath,
            string lTraceTrigger,
            double lTraceMilliseconds,
            int lTraceGlyphCount)
        {
            if (!lTraceSurfaceTable.TryGetValue(lTraceSurface, out LTraceDrawTally? lTraceTally))
            {
                lTraceTally = new LTraceDrawTally();
                lTraceSurfaceTable[lTraceSurface] = lTraceTally;
                lTraceSurfaceOrder.Add(lTraceSurface);
            }

            lTraceCursor = lTraceDrawCursor;
            lTraceSourcePath = string.IsNullOrWhiteSpace(lTraceDrawSourcePath) ? null : lTraceDrawSourcePath;
            lTraceTally.LTraceDrawAdd(lTraceTrigger, lTraceMilliseconds, lTraceGlyphCount);
        }

        internal string LTraceSummaryRead()
        {
            string lTraceCursorText = lTraceCursor.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
            string lTraceSourceText = lTraceSourcePath ?? "(no media)";
            return $"Timeline redrawn for {lTraceCursorText}; {lTraceSourceText}";
        }

        internal string LTraceDetailRead()
        {
            var lTraceBuilder = new StringBuilder();
            foreach (string lTraceSurface in lTraceSurfaceOrder)
            {
                if (lTraceBuilder.Length > 0)
                {
                    lTraceBuilder.Append('\n');
                }

                lTraceBuilder.Append(lTraceSurfaceTable[lTraceSurface].LTimelineDetailRead(lTraceSurface));
            }

            return lTraceBuilder.ToString();
        }
    }
}
