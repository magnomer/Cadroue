using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PList
{
    public IReadOnlyList<string> PListSelectionRead() => LList.LListSelectionRead();

    public void PListSelect(string pListPath)
    {
        if (pListDocket.LDocketItemFind(pListPath) is not null)
        {
            LList.LListSelect(pListPath);
        }
    }

    private void PListPathHandle(string? pListPath)
    {
        PListSelectionUpdate();
        PListPathChange?.Invoke(pListPath);
        PListLockChange?.Invoke(PListLockCheck());
    }

    private void PListSelectionUpdate()
    {
        foreach ((string pRowPath, Border pRowBorder) in pListRows)
        {
            if (pListDocket.LDocketItemFind(pRowPath) is { } pListItem)
            {
                pRowBorder.Background = PListBackgroundRead(pListItem);
            }
        }
    }

    private void PListPressHandle(string pRowPath) =>
        LList.LListPressSelect(
            pRowPath,
            (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift,
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent)
    {
        if (pKeyEvent.Key != Key.A || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        LList.LListAllSelect();
        pKeyEvent.Handled = true;
    }
}
