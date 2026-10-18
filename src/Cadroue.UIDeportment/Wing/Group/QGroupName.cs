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

    public event Action? QGroupNameChanged;

    public int? QGroupEditingIndex => qGroupEditingIndex;

    public void QGroupEditStart(int lIndex)
    {
        if (!QGroupIndexCheck(lIndex) || qGroupEditingIndex == lIndex)
        {
            return;
        }

        qGroupEditingIndex = lIndex;
        QGroupNameChanged?.Invoke();
    }

    public void QGroupEditCancel()
    {
        if (qGroupEditingIndex is null)
        {
            return;
        }

        qGroupEditingIndex = null;
        QGroupNameChanged?.Invoke();
    }

    public bool QGroupNameCommit(string lName)
    {
        if (qGroupEditingIndex is not { } lIndex)
        {
            return false;
        }

        qGroupEditingIndex = null;
        bool lApplied = QGroupNameApply(lIndex, lName);
        QGroupNameChanged?.Invoke();
        return lApplied;
    }

    public bool QGroupNameSet(int lIndex, string lName)
    {
        if (!QGroupNameApply(lIndex, lName))
        {
            return false;
        }

        QGroupNameChanged?.Invoke();
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

    private bool QGroupIndexCheck(int lIndex) => lIndex >= 0 && lIndex < qGroup.QGroupRecords.Count;

    private bool QGroupNameApply(int lIndex, string lName)
    {
        if (QGroupNameValidate(lIndex, lName) is not { } lTrimmed)
        {
            return false;
        }

        QGroupEntrySet(lIndex, lTrimmed);
        return true;
    }

    private string? QGroupNameValidate(int lIndex, string lName)
    {
        string lTrimmed = lName.Trim();
        if (!QGroupIndexCheck(lIndex)
            || lTrimmed.Length == 0
            || string.Equals(lTrimmed, qGroup.QGroupRecords[lIndex].QGroupRecordName, StringComparison.Ordinal))
        {
            return null;
        }

        return lTrimmed;
    }

    private void QGroupEntrySet(int lIndex, string lName) => qGroup.QGroupRecords[lIndex].QGroupRecordName = lName;
}
