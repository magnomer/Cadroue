using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.UIShell.PFlow;

public sealed class LKeyframeNotice
{
    public LKeyframeNotice(
        int lRequestSerial,
        IReadOnlyList<LKeyframeEntry> lKeyframeList,
        IReadOnlyList<LKeyframeScanRange> lScannedRanges)
    {
        if (lRequestSerial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lRequestSerial));
        }

        LKeyframeSerial = lRequestSerial;
        LKeyframeList = (lKeyframeList ?? throw new ArgumentNullException(nameof(lKeyframeList))).ToArray();
        LKeyframeRanges = (lScannedRanges ?? throw new ArgumentNullException(nameof(lScannedRanges))).ToArray();
    }

    public int LKeyframeSerial { get; }
    public IReadOnlyList<LKeyframeEntry> LKeyframeList { get; }
    public IReadOnlyList<LKeyframeScanRange> LKeyframeRanges { get; }
}
