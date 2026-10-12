using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class QGroupDrag
{
    private readonly QGroup qGroup;
    private bool qGroupDragOrigin;
    private double qGroupDragX;
    private double qGroupDragY;
    private QGhost qGroupGhost = new(0, 0);

    public event Action<string>? QGroupItemOpened;
    public event Action<IReadOnlyList<string>>? QGroupPathsRequest;

    public QGroupDrag(QGroup lOwner)
    {
        qGroup = lOwner;
    }

    public QGhost QGroupGhost => qGroupGhost;

    public int QGroupDragIndex => qGroup.QGroupSourceIndex ?? -1;

    public string QGroupDragPath => qGroup.QGroupDragPath ?? string.Empty;

    public void QGroupPressHandle(int lGroupIndex, string lPath, double lX, double lY, double lGrabX, double lGrabY)
    {
        qGroupDragOrigin = true;
        qGroupDragX = lX;
        qGroupDragY = lY;
        qGroupGhost = new QGhost(lGrabX, lGrabY);
        qGroup.QGroupDragSet(lGroupIndex, lPath);
        QGroupItemOpened?.Invoke(lPath);
    }

    public bool QGroupDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (!qGroupDragOrigin || qGroup.QGroupSourceIndex is null || qGroup.QGroupDragPath is null || !lPressed)
        {
            return false;
        }

        return Math.Abs(lX - qGroupDragX) >= lMinimumX || Math.Abs(lY - qGroupDragY) >= lMinimumY;
    }

    public void QGroupDragClear()
    {
        qGroupDragOrigin = false;
        qGroup.QGroupDragSet(null, null);
    }

    public bool QGroupCardAccept(
        int lTargetIndex,
        int lInsertAt,
        string[]? lFilePaths,
        int? lMoveIndex,
        string? lMovePath,
        string[]? lListPaths)
    {
        if (QGroupExternalAccept(lFilePaths))
        {
            return true;
        }

        return lMoveIndex is { } lSourceIndex && lMovePath is { } lPath
            ? qGroup.QGroupItemMove(lSourceIndex, lPath, lTargetIndex, lInsertAt)
            : qGroup.QGroupPathsInsert(lTargetIndex, lListPaths ?? [], lInsertAt);
    }

    public bool QGroupPanelAccept(
        bool lHandled, string[]? lFilePaths, int? lMoveIndex, string? lMovePath, string[]? lListPaths)
    {
        if (lHandled || QGroupExternalAccept(lFilePaths))
        {
            return true;
        }

        string lName = LLocalization.LLocalizationFormat("Group.Default.Name", qGroup.QGroupRecords.Count + 1);
        return lMoveIndex is { } lSourceIndex && lMovePath is { } lPath
            ? qGroup.QGroupAdd([lPath], lName, lSourceIndex)
            : qGroup.QGroupAdd(lListPaths ?? [], lName);
    }

    private bool QGroupExternalAccept(string[]? lFilePaths)
    {
        if (lFilePaths is null)
        {
            return false;
        }

        QGroupPathsRequest?.Invoke(lFilePaths);
        return true;
    }

    public static int QGroupInsertResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
    {
        for (int lIndex = 0; lIndex < lTops.Count; lIndex++)
        {
            if (lY < lTops[lIndex] + (lHeights[lIndex] / 2))
            {
                return lIndex;
            }
        }

        return lTops.Count;
    }
}
