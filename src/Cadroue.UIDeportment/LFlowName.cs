using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LFlowNamePrompt(
    int LFlowNameIndex,
    string LFlowNameText,
    string LFlowNamePrefix,
    string LFlowNameSuffix,
    bool LFlowPrefixShown,
    bool LFlowSuffixShown);

public sealed class LFlowName
{
    private readonly LFlow lFlow;
    private int? lFlowNameIndex;

    public LFlowName(LFlow lOwner)
    {
        lFlow = lOwner;
    }

    public event Action<LFlowNamePrompt>? LFlowNameShow;
    public event Action? LFlowNameClose;

    public int? LFlowNameIndex => lFlowNameIndex;

    public bool LFlowNameStart()
    {
        IReadOnlyList<LPiece> lSections = lFlow.LFlowSection.LFlowSectionsRead();
        if (lFlow.LFlowSection.LFlowSelectionRead() is not int lIndex || lIndex >= lSections.Count)
        {
            return false;
        }

        LFlowNameHide();
        lFlowNameIndex = lIndex;
        LPiece lPiece = lSections[lIndex];
        LFlowNameShow?.Invoke(new LFlowNamePrompt(
            lIndex,
            lPiece.LPieceName,
            lPiece.LPiecePrefix,
            lPiece.LPieceSuffix,
            !string.IsNullOrEmpty(lPiece.LPiecePrefix),
            !string.IsNullOrEmpty(lPiece.LPieceSuffix)));
        return true;
    }

    public void LFlowNameHide()
    {
        if (lFlowNameIndex is null)
        {
            return;
        }

        lFlowNameIndex = null;
        LFlowNameClose?.Invoke();
    }

    public void LFlowNameCommit(string lName, string lPrefix, string lSuffix)
    {
        if (lFlowNameIndex is int lIndex)
        {
            lFlow.LFlowSection.LFlowNameSet(lIndex, lName.Trim(), lPrefix.Trim(), lSuffix.Trim());
        }

        LFlowNameHide();
    }

    public static double LFlowOffsetResolve(bool lEmpty, double lOrigin, double lSize, double lHostSize) =>
        lEmpty ? 0 : lOrigin + lSize / 2 - lHostSize / 2;
}
