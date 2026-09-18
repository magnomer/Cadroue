namespace Cadroue.UIDeportment;

public sealed class LSection
{
    private bool lSectionEditable = true;
    private bool lSectionMinimized;
    private bool lSectionDragActive;
    private int? lSectionEditIndex;
    private int? lSectionDragIndex;

    public event Action<bool>? LSectionMinimizeChange;

    public bool LSectionEditable => lSectionEditable;

    public bool LSectionMinimized => lSectionMinimized;

    public bool LSectionDragActive => lSectionDragActive;

    public int? LSectionEditIndex => lSectionEditIndex;

    public int? LSectionDragIndex => lSectionDragIndex;

    public void LSectionEditableSet(bool lEditable) => lSectionEditable = lEditable;

    public bool LSectionMinimizedSet(bool lMinimized)
    {
        if (lSectionMinimized == lMinimized)
        {
            return false;
        }

        lSectionMinimized = lMinimized;
        LSectionMinimizeChange?.Invoke(lMinimized);
        return true;
    }

    public void LSectionEditSet(int? lEditIndex) => lSectionEditIndex = lEditIndex;

    public void LSectionDragSet(int? lDragIndex, bool lDragActive)
    {
        lSectionDragIndex = lDragIndex;
        lSectionDragActive = lDragActive;
    }

    public bool LSectionDragCheck() =>
        lSectionEditable && lSectionDragIndex is not null && lSectionEditIndex is null;
}
