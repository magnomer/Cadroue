namespace Cadroue.Core;

public readonly partial record struct LSegment(TimeSpan LSegmentStart, TimeSpan LSegmentEnd, int LSegmentColorIndex, string LSegmentName)
{
    private readonly string? lSegmentPrefix;
    private readonly string? lSegmentSuffix;

    public bool LSegmentHidden { get; init; }

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
