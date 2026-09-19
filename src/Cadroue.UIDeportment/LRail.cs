using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LRailRelayOutcome
{
    LRailRelaySkipped,
    LRailRelayBusy,
    LRailRelayRelayed,
    LRailRelayCopied,
    LRailRelayKept
}

public sealed class LRail
{
    private readonly LStrip lStrip;
    private LStripTab? lRailDragTab;
    private double lRailDragX;
    private double lRailDragY;
    private double lRailOffsetX;
    private double lRailOffsetY;
    private bool lRailDragActive;

    public LRail(LStrip lStripOwner)
    {
        lStrip = lStripOwner;
    }

    public LStrip LStrip => lStrip;

    public LStripTab? LRailDragTab => lRailDragTab;

    public bool LRailDragActive => lRailDragActive;

    public double LRailOffsetX => lRailOffsetX;

    public double LRailOffsetY => lRailOffsetY;

    public void LRailHoverSet(LStripTab? lTab)
    {
        if (lTab is not null)
        {
            lStrip.LStripHoverSet(lTab);
        }
    }

    public void LRailHoverClear(LStripTab? lTab)
    {
        if (lTab is not null)
        {
            lStrip.LStripHoverClear(lTab);
        }
    }

    public static bool LRailNameCheck(bool lVisible, double lX, double lY, double lWidth, double lHeight) =>
        lVisible && lX >= 0 && lY >= 0 && lX <= lWidth && lY <= lHeight;

    public bool LRailPressHandle(
        LStripTab? lTab,
        int lClickCount,
        bool lNameHit,
        double lX,
        double lY,
        double lOffsetX,
        double lOffsetY)
    {
        if (lTab is null)
        {
            return false;
        }

        if (lClickCount >= 2 && lNameHit)
        {
            LRailDragClear();
            lStrip.LStripEditSet(lTab, true);
            return false;
        }

        lRailDragTab = lTab;
        lRailDragX = lX;
        lRailDragY = lY;
        lRailOffsetX = lOffsetX;
        lRailOffsetY = lOffsetY;
        lRailDragActive = false;
        lStrip.LStripSelect(lTab);
        return true;
    }

    public bool LRailMoveCheck(bool lLeftPressed) => lRailDragTab is not null && lLeftPressed;

    public bool LRailDragResolve(double lX, double lY, double lMinimumX, double lMinimumY)
    {
        if (lRailDragTab is null || lRailDragActive)
        {
            return false;
        }

        if (Math.Abs(lX - lRailDragX) < lMinimumX && Math.Abs(lY - lRailDragY) < lMinimumY)
        {
            return false;
        }

        lRailDragActive = true;
        return true;
    }

    public void LRailDragMove(double lPointer, IReadOnlyList<double> lCenters)
    {
        if (lRailDragTab is not { } lTab || !lRailDragActive)
        {
            return;
        }

        lStrip.LStripMove(lTab, LRailIndexResolve(lPointer, lCenters));
    }

    public LStripTab? LRailReleaseResolve()
    {
        LStripTab? lReleased = lRailDragActive ? lRailDragTab : null;
        LRailDragClear();
        return lReleased;
    }

    public void LRailDragClear()
    {
        lRailDragTab = null;
        lRailDragActive = false;
    }

    public bool LRailKeyHandle(LStripTab? lTab, bool lEnter, bool lEscape, string lText)
    {
        if (lTab is null)
        {
            return false;
        }

        if (lEnter)
        {
            LRailNameCommit(lTab, lText);
            return true;
        }

        if (lEscape)
        {
            lStrip.LStripEditSet(lTab, false);
            return true;
        }

        return false;
    }

    public void LRailNameCommit(LStripTab lTab, string? lText)
    {
        lStrip.LStripEditSet(lTab, false);
        lStrip.LStripNameSet(lTab, lText);
    }

    public void LRailLeaveHandle(LStripTab? lTab, string? lText)
    {
        if (lTab is { LStripTabEditing: true })
        {
            LRailNameCommit(lTab, lText);
        }
    }

    public void LRailOutsideHandle(LStripTab? lTab, bool lInside, string? lText)
    {
        if (lTab is { LStripTabEditing: true } && !lInside)
        {
            LRailNameCommit(lTab, lText);
        }
    }

    public void LRailCloseHandle(LStripTab? lTab)
    {
        LRailDragClear();
        if (lTab is not null && lStrip.LStripCloseConfirm(lTab))
        {
            lStrip.LStripClose(lTab);
        }
    }

    public static double LRailCenterResolve(bool lVertical, double lX, double lY, double lWidth, double lHeight) =>
        lVertical ? lY + lHeight / 2 : lX + lWidth / 2;

    public double LRailPointerResolve(double lX, double lY) => lStrip.LStripVertical ? lY : lX;

    public static int LRailIndexResolve(double lPointer, IReadOnlyList<double> lCenters)
    {
        if (lCenters.Count == 0)
        {
            return 0;
        }

        int lTarget = 0;
        for (int lIndex = 0; lIndex < lCenters.Count; lIndex++)
        {
            if (lPointer > lCenters[lIndex])
            {
                lTarget = lIndex + 1;
            }
        }

        return Math.Clamp(lTarget, 0, lCenters.Count - 1);
    }

    public static bool LRailInsideCheck(bool lMinimized, double lX, double lY, double lWidth, double lHeight) =>
        !lMinimized && lX >= 0 && lY >= 0 && lX <= lWidth && lY <= lHeight;

    public async Task<LRailRelayOutcome> LRailRelayRun(
        LStripTab? lTab,
        bool lInside,
        double lDipX,
        double lDipY,
        double lDeviceX,
        double lDeviceY)
    {
        if (lTab is null || lInside || lTab.LStripTabPending || lTab.LStripTabWorkspace is not { } lWorkspace)
        {
            return LRailRelayOutcome.LRailRelaySkipped;
        }

        if (lTab.LStripTabBusy)
        {
            LTraceLog.LTraceInfoRecord($"Tab '{lTab.LStripTabTitle}' kept: the worklist is still working");
            return LRailRelayOutcome.LRailRelayBusy;
        }

        LRelay lRelay = lWorkspace.LWorkspaceRelayCreate(lTab.LStripTabCustom, lDipX, lDipY);
        string lRelayFilePath;
        try
        {
            lRelayFilePath = LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay payload could not be written; tab kept", lException);
            return LRailRelayOutcome.LRailRelaySkipped;
        }

        lStrip.LStripPendingSet(lTab, true);
        try
        {
            switch (await LRelayChannel.LRelayDispatch(lRelayFilePath, lDeviceX, lDeviceY))
            {
                case LRelayOutcome.LRelayOutcomeExisting:
                case LRelayOutcome.LRelayOutcomeLaunched:
                    if (lTab.LStripTabBusy)
                    {
                        LTraceLog.LTraceInfoRecord(
                            $"Tab '{lTab.LStripTabTitle}' copied: the worklist started working during the relay");
                        return LRailRelayOutcome.LRailRelayCopied;
                    }

                    LTraceLog.LTraceInfoRecord($"Tab '{lTab.LStripTabTitle}' relayed");
                    lStrip.LStripClose(lTab);
                    return LRailRelayOutcome.LRailRelayRelayed;
                default:
                    LTraceLog.LTraceInfoRecord($"Tab '{lTab.LStripTabTitle}' kept: the other window did not take it");
                    return LRailRelayOutcome.LRailRelayKept;
            }
        }
        finally
        {
            lStrip.LStripPendingSet(lTab, false);
        }
    }
}
