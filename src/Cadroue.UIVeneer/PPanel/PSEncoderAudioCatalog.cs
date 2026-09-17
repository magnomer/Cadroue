using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private static bool PSAudioContainerCheck(string pName, string pContainer) =>
        LRepertoireCatalog.LRepertoireAudioCheck(pName, pContainer);

    private static string[] PSAudioItemsRead() =>
        LRepertoireCatalog.LRepertoireAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.LRepertoireName))
            .Select(pCandidate => pCandidate.LRepertoireText)
            .ToArray();

    private static string[] PSAudioItemsRead(string pContainer)
    {
        if (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer))
        {
            return PSAudioItemsRead();
        }

        return LRepertoireCatalog.LRepertoireAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.LRepertoireName)
                              && PSAudioContainerCheck(pCandidate.LRepertoireName, pContainer))
            .Select(pCandidate => pCandidate.LRepertoireText)
            .ToArray();
    }

    private static string[] PSAudioItemsRead(string pContainer, string pKeep)
    {
        string[] pItems = PSAudioItemsRead(pContainer);
        if (string.IsNullOrEmpty(pKeep) || pItems.Contains(pKeep))
        {
            return pItems;
        }

        string? pName = LRepertoireCatalog.LRepertoireAudioResolve(pKeep);
        bool pFits = pName is not null
            && (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer)
                || PSAudioContainerCheck(pName, pContainer));
        return pFits ? [pKeep, .. pItems] : pItems;
    }

    private static bool PSAudioAvailableCheck(string pText) =>
        LRepertoireCatalog.LRepertoireAudioResolve(pText) is not { } pName
            || LInventory.LInventoryInstalledCheck(pName);
}
