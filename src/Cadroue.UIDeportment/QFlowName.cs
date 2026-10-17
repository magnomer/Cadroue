using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QFlowNamePrompt(
    string QFlowNameText,
    string QFlowNamePrefix,
    string QFlowNameSuffix,
    bool QFlowPrefixShown,
    bool QFlowSuffixShown);

// The rename wiring: hands each rename input to the rename gate, relays its prompt to the surface and places the
// prompt over the section the gate holds. It keeps no rename state.
public sealed class QFlowName
{
    private readonly QFlow qFlow;
    private readonly CFlowName qFlowNameGate;

    public QFlowName(QFlow lOwner, CFlowName lGate)
    {
        qFlow = lOwner;
        qFlowNameGate = lGate;
        lGate.CFlowNameShown += QFlowNameRaise;
        lGate.CFlowNameClosed += QFlowCloseRaise;
    }

    public event Action<QFlowNamePrompt>? QFlowNameShown;
    public event Action? QFlowNameClosed;

    public int? QFlowNameIndex => qFlowNameGate.CFlowNameIndex;

    public bool QFlowNameStart() => qFlowNameGate.CFlowNameStart();

    public void QFlowNameHide() => qFlowNameGate.CFlowNameHide();

    public void QFlowNameCommit(string lName, string lPrefix, string lSuffix) =>
        qFlowNameGate.CFlowNameCommit(qFlow.QFlowEdit.QFlowSectionEditable, lName, lPrefix, lSuffix);

    // The prompt's place over the section the open rename shows, in a host of this size.
    public QViewfinderPoint QFlowPopupResolve(double lWidth, double lHeight) =>
        qFlow.QFlowViewfinder.QViewfinderPopupResolve(qFlowNameGate.CFlowNameIndex ?? -1, lWidth, lHeight);

    private void QFlowNameRaise() =>
        QFlowNameShown?.Invoke(new QFlowNamePrompt(
            qFlowNameGate.CFlowNameText,
            qFlowNameGate.CFlowNamePrefix,
            qFlowNameGate.CFlowNameSuffix,
            qFlowNameGate.CFlowPrefixShown,
            qFlowNameGate.CFlowSuffixShown));

    private void QFlowCloseRaise() => QFlowNameClosed?.Invoke();

    public static double QFlowOffsetResolve(bool lEmpty, double lOrigin, double lSize, double lHostSize) =>
        lEmpty ? 0 : lOrigin + lSize / 2 - lHostSize / 2;
}
