namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private void PFlowSectionAdd()
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        if (lCursor >= lSpool.LSpoolDuration) return;
        lSectionList.Add(new LSectionEntry(lCursor, lSpool.LSpoolDuration, PFlowSectionColorIndexNext(), string.Empty));
        lSectionIndexSelect = lSectionList.Count - 1;
        PFlowSectionPropagateUpdate();
    }

    private void PFlowSectionStartSet()
    {
        if (lSpool is null) return;
        if (lSectionIndexSelect is null) { PFlowSectionAdd(); return; }
        LSectionEntry section = lSectionList[lSectionIndexSelect.Value];
        if (section.LSectionEnd < lCursor) { PFlowSectionAdd(); return; }
        if (lCursor >= section.LSectionEnd) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSectionStart = lCursor };
        PFlowSectionPropagateUpdate();
    }

    private void PFlowSectionSplit()
    {
        if (lSectionIndexSelect is null) return;
        LSectionEntry section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSectionStart || lCursor >= section.LSectionEnd) return;
        int index = lSectionIndexSelect.Value;
        int secondColorIndex = PFlowSectionColorIndexNext();
        lSectionList.RemoveAt(index);
        lSectionList.Insert(index, new LSectionEntry(lCursor, section.LSectionEnd, secondColorIndex, string.Empty));
        lSectionList.Insert(index, section with { LSectionEnd = lCursor });
        lSectionIndexSelect = index;
        PFlowSectionPropagateUpdate();
    }

    private void PFlowSectionEndSet()
    {
        if (lSectionIndexSelect is null) return;
        LSectionEntry section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSectionStart) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSectionEnd = lCursor };
        PFlowSectionPropagateUpdate();
    }

    private void PFlowSectionDelete()
    {
        if (lSectionIndexSelect is null) return;
        int index = lSectionIndexSelect.Value;
        lSectionList.RemoveAt(index);
        lSectionIndexSelect = lSectionList.Count == 0 ? null : Math.Min(index, lSectionList.Count - 1);
        PFlowSectionPropagateUpdate();
    }

    private void PFlowSectionPropagateUpdate()
    {
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexSelect);
        PFlowSectionChangeNotice?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
    }

    public void PFlowSectionSelectRequest(int pSectionIndex)
    {
        PFlowViewfinderSectionSelectHandle(pSectionIndex);
    }

    public void PFlowSectionNameChangeRequest(int pSectionIndex, string pSectionName)
    {
        if (pSectionIndex < 0 || pSectionIndex >= lSectionList.Count) return;
        lSectionList[pSectionIndex] = lSectionList[pSectionIndex] with { LSectionName = pSectionName };
    }

    private int PFlowSectionColorIndexNext() => lSectionList.Count % LSectionPaletteCount;

    private void PFlowViewfinderSectionSelectHandle(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= lSectionList.Count) return;
        lSectionIndexSelect = sectionIndex;
        PFlowSectionPropagateUpdate();
    }
}
