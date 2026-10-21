namespace Cadroue.UIDeportment;

// The curve canvas's GUI-only drag lifecycle: each press and move relays one request to Conduct.
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
        qCurve.QCurvePressHandle(lX, lY, lSize);
        QCurveDragSet(true);
    }

    private void QCurveDragSet(bool lActive) => qCurveDragActive = lActive;

    public bool QCurveMoveHandle(double lX, double lY, double lSize)
    {
        (double lInput, double lOutput) = QCurveCanvas.QCurveValueResolve(lX, lY, lSize);
        return qCurve.QCurveMoveHandle(lInput, lOutput, qCurveDragActive);
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
