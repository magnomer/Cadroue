using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed class LKeyframeNotice
{
    public LKeyframeNotice(
        int lRequestSerial,
        IReadOnlyList<LKeyframeEntry> lKeyframes,
        IReadOnlyList<LKeyframeScanRange> lScannedRanges)
    {
        if (lRequestSerial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lRequestSerial));
        }

        LRequestSerial = lRequestSerial;
        LKeyframes = (lKeyframes ?? throw new ArgumentNullException(nameof(lKeyframes))).ToArray();
        LScannedRanges = (lScannedRanges ?? throw new ArgumentNullException(nameof(lScannedRanges))).ToArray();
    }

    public int LRequestSerial { get; }
    public IReadOnlyList<LKeyframeEntry> LKeyframes { get; }
    public IReadOnlyList<LKeyframeScanRange> LScannedRanges { get; }
}
