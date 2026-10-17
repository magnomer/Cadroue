namespace Cadroue.UIDeportment;

public sealed class QListSuccessor
{
    private string? qListPathSuccessor;

    public string? QListPathSuccessor => qListPathSuccessor;

    public void QListSuccessorSet(IReadOnlyList<string> lRemovedPaths, IReadOnlyList<string> lPaths, int lRemovedIndex)
    {
        QListSuccessorReset();
        QListSuccessorApply(QListSuccessorResolve(lRemovedPaths, lPaths, lRemovedIndex));
    }

    public void QListSuccessorReset() => qListPathSuccessor = null;

    private void QListSuccessorApply(string? lSuccessor) => qListPathSuccessor = lSuccessor;

    private static string? QListSuccessorResolve(
        IReadOnlyList<string> lRemovedPaths, IReadOnlyList<string> lPaths, int lRemovedIndex)
    {
        var lRemovedSet = new HashSet<string>(lRemovedPaths, StringComparer.OrdinalIgnoreCase);
        for (int lIndex = lRemovedIndex; lIndex < lPaths.Count; lIndex++)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                return lPaths[lIndex];
            }
        }

        for (int lIndex = lRemovedIndex - 1; lIndex >= 0; lIndex--)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                return lPaths[lIndex];
            }
        }

        return null;
    }
}
