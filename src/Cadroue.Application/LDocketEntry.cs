namespace Cadroue.Application;

public sealed class LDocketEntry
{
    public LDocketEntry(string lDocketEntryPath, Guid lDocketEntryBatch = default)
    {
        LDocketEntryPath = lDocketEntryPath;
        LDocketEntryBatch = lDocketEntryBatch;
    }

    public string LDocketEntryPath { get; }

    public Guid LDocketEntryBatch { get; set; }

    public bool LDocketEntryDelivered { get; set; }

    public bool LDocketEntryLocked { get; set; }
}
