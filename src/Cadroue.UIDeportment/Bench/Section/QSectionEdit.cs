using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The section row editor's wiring: hands the edited text, the editor's keys and its focus loss to the edit gate.
public sealed class QSectionEdit
{
    private const string QSectionReturnKey = "Return";
    private const string QSectionEscapeKey = "Escape";

    private readonly CSectionEdit qSectionEditGate;

    public QSectionEdit(CSectionEdit lGate)
    {
        qSectionEditGate = lGate;
    }

    // Forwarded for the row surface's editor.
    public int QSectionEditSerial => qSectionEditGate.CSectionEditSerial;

    public void QSectionTextSet(string lName, string lPrefix, string lSuffix) =>
        qSectionEditGate.CSectionEditSet(lName, lPrefix, lSuffix);

    // The editor's keys: Return commits, Escape drops; any other key is not the editor's.
    public bool QSectionKeyRun(string lKey)
    {
        if (string.Equals(lKey, QSectionReturnKey, StringComparison.Ordinal))
        {
            qSectionEditGate.CSectionEditCommit();
            return true;
        }

        if (string.Equals(lKey, QSectionEscapeKey, StringComparison.Ordinal))
        {
            qSectionEditGate.CSectionEditCancel();
            return true;
        }

        return false;
    }

    public void QSectionBlurHandle(int lSerial, bool lFocusInside) =>
        qSectionEditGate.CSectionEditHandle(lSerial, lFocusInside);
}
