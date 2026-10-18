using System.Windows;

namespace Cadroue.UIVeneer;

internal static class PTabLock
{
    public static void PTabLockAttach(PList pList, params UIElement[] pEditors)
    {
        Action<bool> pTabLockApply = pLocked => PTabLockApply(pEditors, pLocked);
        PTabListAttach(pList, pTabLockApply);
        pTabLockApply(pList.QList.QListLockCheck());
    }

    private static void PTabLockApply(UIElement[] pEditors, bool pLocked) =>
        pEditors.ToList().ForEach(pEditor => pEditor.IsEnabled = !pLocked);

    private static void PTabListAttach(PList pList, Action<bool> pTabLockApply) =>
        pList.QList.QListLockChanged += pTabLockApply;
}
