namespace Cadroue.UIDeportment;

public sealed class QListSuccessor
{
    private string? qListPathSuccessor;

    public string? QListPathSuccessor => qListPathSuccessor;

    public void QListSuccessorSet(IReadOnlyList<string> lRemovedPaths, IReadOnlyList<string> lPaths, int lRemovedIndex)
    {
        var lRemovedSet = new HashSet<string>(lRemovedPaths, StringComparer.OrdinalIgnoreCase);
        qListPathSuccessor = null;
        for (int lIndex = lRemovedIndex; lIndex < lPaths.Count; lIndex++)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                qListPathSuccessor = lPaths[lIndex];
                return;
            }
        }

        for (int lIndex = lRemovedIndex - 1; lIndex >= 0; lIndex--)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                qListPathSuccessor = lPaths[lIndex];
                return;
            }
        }
    }

    public void QListSuccessorReset() => qListPathSuccessor = null;
}
