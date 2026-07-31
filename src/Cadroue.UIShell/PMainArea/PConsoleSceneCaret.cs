using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PConsole
{
    private void PConsoleCaretAttach()
    {
        pConsoleRelayCombo.ApplyTemplate();
        if (pConsoleRelayCombo.Template?.FindName("PART_EditableTextBox", pConsoleRelayCombo) is not TextBox pEditableBox)
        {
            return;
        }

        pEditableBox.IsReadOnly = true;
        pEditableBox.PreviewMouseLeftButtonDown += PConsoleReadonlyClear;
        pEditableBox.LostKeyboardFocus += PConsoleReadonlySet;
    }

    private void PConsoleCaretSet()
    {
        if (pConsoleRelayCombo.Template?.FindName("PART_EditableTextBox", pConsoleRelayCombo) is TextBox pEditableBox)
        {
            pEditableBox.IsReadOnly = true;
        }
    }

    private void PConsoleReadonlyClear(object pSender, MouseButtonEventArgs pArguments)
    {
        if (pSender is TextBox pEditableBox)
        {
            pEditableBox.IsReadOnly = false;
        }
    }

    private void PConsoleReadonlySet(object pSender, KeyboardFocusChangedEventArgs pArguments)
    {
        if (pSender is TextBox pEditableBox)
        {
            pEditableBox.IsReadOnly = true;
        }
    }

    private void PConsolePressHandle(object pSender, MouseButtonEventArgs pArguments)
    {
        if (pConsoleRelayCombo.IsDropDownOpen || !pConsoleRelayCombo.IsKeyboardFocusWithin)
        {
            return;
        }

        if (pArguments.OriginalSource is DependencyObject pSource && PConsoleComboCheck(pSource))
        {
            return;
        }

        PConsoleFocusClear();
    }

    private void PConsoleDeactivateHandle(object? pSender, EventArgs pArguments) => PConsoleFocusClear();

    private void PConsoleFocusClear()
    {
        if (!pConsoleRelayCombo.IsKeyboardFocusWithin)
        {
            return;
        }

        pConsoleRelayCombo.IsDropDownOpen = false;
        Keyboard.ClearFocus();
    }

    private bool PConsoleComboCheck(DependencyObject pSource)
    {
        for (DependencyObject? pNode = pSource;
            pNode is not null;
            pNode = VisualTreeHelper.GetParent(pNode) ?? LogicalTreeHelper.GetParent(pNode))
        {
            if (ReferenceEquals(pNode, pConsoleRelayCombo))
            {
                return true;
            }
        }

        return false;
    }
}
