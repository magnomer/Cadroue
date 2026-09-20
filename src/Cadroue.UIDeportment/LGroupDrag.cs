using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LGroupDrag
{
    private readonly LGroup lGroup;
    private bool lGroupDragOrigin;
    private double lGroupDragX;
    private double lGroupDragY;
    private double lGroupGrabX;
    private double lGroupGrabY;

    public event Action<string>? LGroupItemOpen;
    public event Action<IReadOnlyList<string>>? LGroupPathsRequest;

    public LGroupDrag(LGroup lOwner)
    {
        lGroup = lOwner;
    }

    public double LGroupGrabX => lGroupGrabX;

    public double LGroupGrabY => lGroupGrabY;

    public int LGroupDragIndex => lGroup.LGroupSourceIndex ?? -1;

    public string LGroupDragPath => lGroup.LGroupDragPath ?? string.Empty;

    public void LGroupPressHandle(int lGroupIndex, string lPath, double lX, double lY, double lGrabX, double lGrabY)
    {
        lGroupDragOrigin = true;
        lGroupDragX = lX;
        lGroupDragY = lY;
        lGroupGrabX = lGrabX;
        lGroupGrabY = lGrabY;
        lGroup.LGroupDragSet(lGroupIndex, lPath);
        LGroupItemOpen?.Invoke(lPath);
    }

    public bool LGroupDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (!lGroupDragOrigin || lGroup.LGroupSourceIndex is null || lGroup.LGroupDragPath is null || !lPressed)
        {
            return false;
        }

        return Math.Abs(lX - lGroupDragX) >= lMinimumX || Math.Abs(lY - lGroupDragY) >= lMinimumY;
    }

    public void LGroupDragClear()
    {
        lGroupDragOrigin = false;
        lGroup.LGroupDragSet(null, null);
    }

    public bool LGroupCardAccept(
        int lTargetIndex,
        int lInsertAt,
        string[]? lFilePaths,
        int? lMoveIndex,
        string? lMovePath,
        string[]? lListPaths)
    {
        if (LGroupExternalAccept(lFilePaths))
        {
            return true;
        }

        return lMoveIndex is { } lSourceIndex && lMovePath is { } lPath
            ? lGroup.LGroupItemMove(lSourceIndex, lPath, lTargetIndex, lInsertAt)
            : lGroup.LGroupPathsInsert(lTargetIndex, lListPaths ?? [], lInsertAt);
    }

    public bool LGroupPanelAccept(
        bool lHandled, string[]? lFilePaths, int? lMoveIndex, string? lMovePath, string[]? lListPaths)
    {
        if (lHandled || LGroupExternalAccept(lFilePaths))
        {
            return true;
        }

        string lName = LLocalization.LLocalizationFormat("Group.Default.Name", lGroup.LGroupRecords.Count + 1);
        return lMoveIndex is { } lSourceIndex && lMovePath is { } lPath
            ? lGroup.LGroupAdd([lPath], lName, lSourceIndex)
            : lGroup.LGroupAdd(lListPaths ?? [], lName);
    }

    private bool LGroupExternalAccept(string[]? lFilePaths)
    {
        if (lFilePaths is null)
        {
            return false;
        }

        LGroupPathsRequest?.Invoke(lFilePaths);
        return true;
    }

    public static int LGroupInsertResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
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
