using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QRailRelayOutcome
{
    QRailRelaySkipped,
    QRailRelayBusy,
    QRailRelayRelayed,
    QRailRelayCopied,
    QRailRelayKept
}

public sealed class QRail
{
    private readonly QStrip qStrip;
    private QStripTab? qRailDragTab;
    private double qRailDragX;
    private double qRailDragY;
    private QGhost qRailGhost = new(0, 0);
    private bool qRailDragActive;

    internal QRail(QStrip lStripOwner)
    {
        qStrip = lStripOwner;
    }

    public QStrip QStrip => qStrip;

    public QStripTab? QRailDragTab => qRailDragTab;

    public bool QRailDragActive => qRailDragActive;

    public QGhost QRailGhost => qRailGhost;

    public void QRailHoverSet(QStripTab? lTab)
    {
        if (lTab is not null)
        {
            qStrip.QStripSeparator.QStripHoverSet(lTab);
        }
    }

    public void QRailHoverClear(QStripTab? lTab)
    {
        if (lTab is not null)
        {
            qStrip.QStripSeparator.QStripHoverClear(lTab);
        }
    }

    public static bool QRailNameCheck(bool lVisible, double lX, double lY, double lWidth, double lHeight) =>
        lVisible && lX >= 0 && lY >= 0 && lX <= lWidth && lY <= lHeight;

    public bool QRailPressHandle(
        QStripTab? lTab,
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
            QRailDragClear();
            qStrip.QStripEditSet(lTab, true);
            return false;
        }

        qRailDragTab = lTab;
        qRailDragX = lX;
        qRailDragY = lY;
        qRailGhost = new QGhost(lOffsetX, lOffsetY);
        qRailDragActive = false;
        qStrip.QStripSelect(lTab);
        return true;
    }

    public bool QRailMoveCheck(bool lLeftPressed) => qRailDragTab is not null && lLeftPressed;

    public bool QRailDragResolve(double lX, double lY, double lMinimumX, double lMinimumY)
    {
        if (qRailDragTab is null || qRailDragActive)
        {
            return false;
        }

        if (Math.Abs(lX - qRailDragX) < lMinimumX && Math.Abs(lY - qRailDragY) < lMinimumY)
        {
            return false;
        }

        qRailDragActive = true;
        return true;
    }

    public void QRailDragMove(double lPointer, IReadOnlyList<double> lCenters)
    {
        if (qRailDragTab is not { } lTab || !qRailDragActive)
        {
            return;
        }

        qStrip.QStripMove(lTab, QRailIndexResolve(lPointer, lCenters));
    }

    public QStripTab? QRailReleaseResolve()
    {
        QStripTab? lReleased = qRailDragActive ? qRailDragTab : null;
        QRailDragClear();
        return lReleased;
    }

    public void QRailDragClear()
    {
        qRailDragTab = null;
        qRailDragActive = false;
    }

    public bool QRailKeyHandle(QStripTab? lTab, bool lEnter, bool lEscape, string lText)
    {
        if (lTab is null)
        {
            return false;
        }

        if (lEnter)
        {
            QRailNameCommit(lTab, lText);
            return true;
        }

        if (lEscape)
        {
            qStrip.QStripEditSet(lTab, false);
            return true;
        }

        return false;
    }

    public void QRailNameCommit(QStripTab lTab, string? lText)
    {
        qStrip.QStripEditSet(lTab, false);
        qStrip.QStripNameSet(lTab, lText);
    }

    public void QRailLeaveHandle(QStripTab? lTab, string? lText)
    {
        if (lTab is { QStripTabEditing: true })
        {
            QRailNameCommit(lTab, lText);
        }
    }

    public void QRailOutsideHandle(QStripTab? lTab, bool lInside, string? lText)
    {
        if (lTab is { QStripTabEditing: true } && !lInside)
        {
            QRailNameCommit(lTab, lText);
        }
    }

    public void QRailCloseHandle(QStripTab? lTab)
    {
        QRailDragClear();
        if (lTab is not null && qStrip.QStripClosure.QStripCloseConfirm(lTab))
        {
            qStrip.QStripClosure.QStripClose(lTab);
        }
    }

    public static double QRailCenterResolve(bool lVertical, double lX, double lY, double lWidth, double lHeight) =>
        lVertical ? lY + lHeight / 2 : lX + lWidth / 2;

    public double QRailPointerResolve(double lX, double lY) => qStrip.QStripSeparator.QStripVertical ? lY : lX;

    public static int QRailIndexResolve(double lPointer, IReadOnlyList<double> lCenters)
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

    public static bool QRailInsideCheck(bool lMinimized, double lX, double lY, double lWidth, double lHeight) =>
        !lMinimized && lX >= 0 && lY >= 0 && lX <= lWidth && lY <= lHeight;

    public async Task<QRailRelayOutcome> QRailRelayRun(
        QStripTab? lTab,
        bool lInside,
        double lDipX,
        double lDipY,
        double lDeviceX,
        double lDeviceY)
    {
        if (lTab is null || lInside || lTab.QStripTabPending || lTab.QStripTabWorkspace is not { } lWorkspace)
        {
            return QRailRelayOutcome.QRailRelaySkipped;
        }

        if (lTab.QStripTabBusy)
        {
            LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' kept: the worklist is still working");
            return QRailRelayOutcome.QRailRelayBusy;
        }

        LRelay lRelay = lWorkspace.QWorkspaceRelayCreate(lTab.QStripTabCustom, lDipX, lDipY);
        string lRelayFilePath;
        try
        {
            lRelayFilePath = LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay payload could not be written; tab kept", lException);
            return QRailRelayOutcome.QRailRelaySkipped;
        }

        qStrip.QStripPendingSet(lTab, true);
        try
        {
            switch (await LRelayChannel.LRelayDispatch(lRelayFilePath, lDeviceX, lDeviceY))
            {
                case LRelayOutcome.LRelayOutcomeExisting:
                case LRelayOutcome.LRelayOutcomeLaunched:
                    if (lTab.QStripTabBusy)
                    {
                        LTraceLog.LTraceInfoRecord(
                            $"Tab '{lTab.QStripTabTitle}' copied: the worklist started working during the relay");
                        return QRailRelayOutcome.QRailRelayCopied;
                    }

                    LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' relayed");
                    qStrip.QStripClosure.QStripClose(lTab);
                    return QRailRelayOutcome.QRailRelayRelayed;
                default:
                    LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' kept: the other window did not take it");
                    return QRailRelayOutcome.QRailRelayKept;
            }
        }
        finally
        {
            qStrip.QStripPendingSet(lTab, false);
        }
    }
}
