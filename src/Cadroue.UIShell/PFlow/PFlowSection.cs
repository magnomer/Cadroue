namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private void PFlowSectionAdd()
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        if (lCursor >= lSpool.LSpoolDuration) return;
        lSectionList.Add(new LSegment(lCursor, lSpool.LSpoolDuration, PFlowColorRead(), string.Empty));
        lSectionIndexSelect = lSectionList.Count - 1;
        PFlowSectionUpdate();
    }

    private void PFlowStartSet()
    {
        if (lSpool is null) return;
        if (lSectionIndexSelect is null) { PFlowSectionAdd(); return; }
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (section.LSegmentEnd < lCursor) { PFlowSectionAdd(); return; }
        if (lCursor >= section.LSegmentEnd) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSegmentStart = lCursor };
        PFlowSectionUpdate();
    }

    private void PFlowSectionSplit()
    {
        if (lSectionIndexSelect is null) return;
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSegmentStart || lCursor >= section.LSegmentEnd) return;
        int index = lSectionIndexSelect.Value;
        int secondColorIndex = PFlowColorRead();
        lSectionList.RemoveAt(index);
        lSectionList.Insert(index, new LSegment(lCursor, section.LSegmentEnd, secondColorIndex, string.Empty));
        lSectionList.Insert(index, section with { LSegmentEnd = lCursor });
        lSectionIndexSelect = index;
        PFlowSectionUpdate();
    }

    private void PFlowEndSet()
    {
        if (lSectionIndexSelect is null) return;
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSegmentStart) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSegmentEnd = lCursor };
        PFlowSectionUpdate();
    }

    private void PFlowSectionDelete()
    {
        if (lSectionIndexSelect is null) return;
        int index = lSectionIndexSelect.Value;
        lSectionList.RemoveAt(index);
        lSectionIndexSelect = lSectionList.Count == 0 ? null : Math.Min(index, lSectionList.Count - 1);
        PFlowSectionUpdate();
    }

    private void PFlowSectionUpdate()
    {
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexSelect);
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
    }

    public void PFlowSectionSelect(int pSectionIndex)
    {
        PFlowViewfinderSelect(pSectionIndex);
    }

    public void PFlowNameSet(int pSectionIndex, string pSectionName)
    {
        if (pSectionIndex < 0 || pSectionIndex >= lSectionList.Count) return;
        lSectionList[pSectionIndex] = lSectionList[pSectionIndex] with { LSegmentName = pSectionName };
    }

    private int PFlowColorRead() => lSectionList.Count % LSectionPaletteCount;

    private void PFlowViewfinderSelect(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= lSectionList.Count) return;
        lSectionIndexSelect = sectionIndex;
        PFlowSectionUpdate();
    }
}
