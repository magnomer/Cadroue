namespace Cadroue.Core;

public sealed class LKeyframeNotice
{
    public LKeyframeNotice(
        int lRequestSerial,
        IReadOnlyList<LKeyframeEntry> lKeyframeList,
        IReadOnlyList<LKeyframeScanRange> lScannedRanges,
        LKeyframeKind lKeyframeKind = LKeyframeKind.LKeyframeKindInter)
    {
        if (lRequestSerial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lRequestSerial));
        }

        LKeyframeSerial = lRequestSerial;
        LKeyframeList = (lKeyframeList ?? throw new ArgumentNullException(nameof(lKeyframeList))).ToArray();
        LKeyframeRanges = (lScannedRanges ?? throw new ArgumentNullException(nameof(lScannedRanges))).ToArray();
        LKeyframeKind = lKeyframeKind;
    }

    public int LKeyframeSerial { get; }
    public IReadOnlyList<LKeyframeEntry> LKeyframeList { get; }
    public IReadOnlyList<LKeyframeScanRange> LKeyframeRanges { get; }
    public LKeyframeKind LKeyframeKind { get; }
}
