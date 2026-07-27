namespace Cadroue.UIShell.PFlow;

public readonly record struct LSegment(TimeSpan LSegmentStart, TimeSpan LSegmentEnd, int LSegmentColorIndex, string LSegmentName)
{
    private readonly string? lSegmentPrefix;
    private readonly string? lSegmentSuffix;

    public string LSegmentPrefix
    {
        get => lSegmentPrefix ?? string.Empty;
        init => lSegmentPrefix = value;
    }

    public string LSegmentSuffix
    {
        get => lSegmentSuffix ?? string.Empty;
        init => lSegmentSuffix = value;
    }
}
