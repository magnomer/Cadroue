namespace Cadroue.UIDeportment;

public sealed class QGroupName
{
    private const string QGroupReturnKey = "Return";
    private const string QGroupEscapeKey = "Escape";

    private readonly QGroup qGroup;
    private int? qGroupEditingIndex;

    public QGroupName(QGroup lOwner)
    {
        qGroup = lOwner;
    }

    public int? QGroupEditingIndex => qGroupEditingIndex;

    public void QGroupEditStart(int lIndex)
    {
        if (!qGroup.QGroupIndexCheck(lIndex) || qGroupEditingIndex == lIndex)
        {
            return;
        }

        qGroupEditingIndex = lIndex;
        qGroup.QGroupChangedRaise();
    }

    public void QGroupEditCancel()
    {
        if (qGroupEditingIndex is null)
        {
            return;
        }

        qGroupEditingIndex = null;
        qGroup.QGroupChangedRaise();
    }

    public bool QGroupNameCommit(string lName)
    {
        if (qGroupEditingIndex is not { } lIndex)
        {
            return false;
        }

        qGroupEditingIndex = null;
        bool lApplied = QGroupNameApply(lIndex, lName);
        qGroup.QGroupChangedRaise();
        return lApplied;
    }

    public bool QGroupNameSet(int lIndex, string lName)
    {
        if (!QGroupNameApply(lIndex, lName))
        {
            return false;
        }

        qGroup.QGroupChangedRaise();
        return true;
    }

    public bool QGroupLabelHandle(int lIndex, int lClickCount)
    {
        if (lClickCount != 2)
        {
            return false;
        }

        QGroupEditStart(lIndex);
        return true;
    }

    public bool QGroupKeyRun(string lKey, string lText)
    {
        if (string.Equals(lKey, QGroupReturnKey, StringComparison.Ordinal))
        {
            QGroupNameCommit(lText);
            return true;
        }

        if (string.Equals(lKey, QGroupEscapeKey, StringComparison.Ordinal))
        {
            QGroupEditCancel();
            return true;
        }

        return false;
    }

    internal void QGroupEditClear() => qGroupEditingIndex = null;

    private bool QGroupNameApply(int lIndex, string lName)
    {
        string lTrimmed = lName.Trim();
        if (!qGroup.QGroupIndexCheck(lIndex)
            || lTrimmed.Length == 0
            || string.Equals(lTrimmed, qGroup.QGroupRecords[lIndex].QGroupRecordName, StringComparison.Ordinal))
        {
            return false;
        }

        qGroup.QGroupRecords[lIndex].QGroupRecordName = lTrimmed;
        return true;
    }
}
