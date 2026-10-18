namespace Cadroue.UIDeportment;

// The curve canvas's drag: a press picks the point under the pointer or adds one there, and while the drag lasts each
// move sets the shown point to the pointer.
public sealed class QCurveDrag
{
    private readonly QCurve qCurve;
    private bool qCurveDragActive;

    public QCurveDrag(QCurve lOwner)
    {
        qCurve = lOwner;
    }

    public bool QCurveDragActive => qCurveDragActive;

    public void QCurvePressHandle(double lX, double lY, double lSize)
    {
        int lHit = qCurve.QCurveCanvas.QCurveHitFind(lX, lY, lSize);
        if (lHit < 0)
        {
            (double lInput, double lOutput) = QCurveCanvas.QCurveValueResolve(lX, lY, lSize);
            qCurve.QCurvePointAdd(lInput, lOutput);
        }
        else
        {
            qCurve.QCurvePointSelect(lHit);
        }

        QCurveDragSet(true);
    }

    private void QCurveDragSet(bool lActive) => qCurveDragActive = lActive;

    public bool QCurveMoveHandle(double lX, double lY, double lSize)
    {
        if (!qCurveDragActive)
        {
            return false;
        }

        (double lInput, double lOutput) = QCurveCanvas.QCurveValueResolve(lX, lY, lSize);
        qCurve.QCurvePointSet(lInput, lOutput);
        return true;
    }

    public bool QCurveReleaseHandle()
    {
        if (!qCurveDragActive)
        {
            return false;
        }

        qCurveDragActive = false;
        return true;
    }
}
