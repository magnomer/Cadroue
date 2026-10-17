using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QFlowNamePrompt(
    int QFlowNameIndex,
    string QFlowNameText,
    string QFlowNamePrefix,
    string QFlowNameSuffix,
    bool QFlowPrefixShown,
    bool QFlowSuffixShown);

public sealed class QFlowName
{
    private readonly QFlow qFlow;
    private int? qFlowNameIndex;

    public QFlowName(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public event Action<QFlowNamePrompt>? QFlowNameShown;
    public event Action? QFlowNameClosed;

    public int? QFlowNameIndex => qFlowNameIndex;

    public bool QFlowNameStart()
    {
        IReadOnlyList<LPiece> lSections = qFlow.QFlowSection.QFlowSectionsRead();
        if (qFlow.QFlowSection.QFlowSelectionRead() is not int lIndex || lIndex >= lSections.Count)
        {
            return false;
        }

        QFlowNameHide();
        QFlowNameShow(QFlowPromptCreate(lIndex, lSections[lIndex]));
        return true;
    }

    private static QFlowNamePrompt QFlowPromptCreate(int lIndex, LPiece lPiece) =>
        new(
            lIndex,
            lPiece.LPieceName,
            lPiece.LPiecePrefix,
            lPiece.LPieceSuffix,
            !string.IsNullOrEmpty(lPiece.LPiecePrefix),
            !string.IsNullOrEmpty(lPiece.LPieceSuffix));

    private void QFlowNameShow(QFlowNamePrompt lPrompt)
    {
        qFlowNameIndex = lPrompt.QFlowNameIndex;
        QFlowNameShown?.Invoke(lPrompt);
    }

    public void QFlowNameHide()
    {
        if (qFlowNameIndex is null)
        {
            return;
        }

        qFlowNameIndex = null;
        QFlowNameClosed?.Invoke();
    }

    public void QFlowNameCommit(string lName, string lPrefix, string lSuffix)
    {
        if (qFlowNameIndex is int lIndex)
        {
            qFlow.QFlowSection.QFlowNameSet(
                lIndex, QFlowNameNormalize(lName), QFlowNameNormalize(lPrefix), QFlowNameNormalize(lSuffix));
        }

        QFlowNameHide();
    }

    private static string QFlowNameNormalize(string lText) => lText.Trim();

    public static double QFlowOffsetResolve(bool lEmpty, double lOrigin, double lSize, double lHostSize) =>
        lEmpty ? 0 : lOrigin + lSize / 2 - lHostSize / 2;
}
