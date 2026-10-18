using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderNameChip
{
    private readonly RichTextBox psNameBox;
    private readonly Window psEncoderWindow;

    internal PSEncoderNameChip(RichTextBox pNameBox, Window pWindow)
    {
        psNameBox = pNameBox;
        psEncoderWindow = pWindow;
    }

    internal UIElement PSNameTokenBuild(string pLabel, string pToken)
    {
        Border pBorder = PSNameChipBuild(PSNameLabelBuild(pLabel), 10);
        PSNameChipAttach(pBorder, () => pToken, null);
        return pBorder;
    }

    internal UIElement PSNameOperatorBuild(string pWord, string pKind)
    {
        TextBox psNumberBox = PSNameCountBuild();
        PSNameCountAttach(psNumberBox);
        Border pBorder = PSNameChipBuild(PSNameContentBuild(pWord, psNumberBox), 6);
        PSNameChipAttach(pBorder, () => PSNameOperatorFormat(pKind, psNumberBox.Text), psNumberBox);
        return pBorder;
    }

    private static TextBlock PSNameLabelBuild(string pLabel) => new()
    {
        Text = pLabel,
        Foreground = PSField.PSFieldText,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static TextBox PSNameCountBuild() => new()
    {
        Text = "1",
        Width = 26,
        MaxLength = 3,
        TextAlignment = TextAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        BorderThickness = new Thickness(0),
        Background = Brushes.Transparent,
        Margin = new Thickness(4, 0, 0, 0)
    };

    private static void PSNameCountAttach(TextBox psNumberBox)
    {
        psNumberBox.PreviewTextInput += (_, e) => e.Handled = !PSNameDigitsCheck(e.Text);
        psNumberBox.LostFocus += (_, _) => psNumberBox.Text = PSNameCountRead(psNumberBox.Text).ToString();
    }

    private static StackPanel PSNameContentBuild(string pWord, TextBox psNumberBox)
    {
        var pWordText = new TextBlock
        {
            Text = pWord,
            Foreground = PSField.PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pContent = new StackPanel { Orientation = Orientation.Horizontal };
        pContent.Children.Add(pWordText);
        pContent.Children.Add(psNumberBox);
        return pContent;
    }

    private static Border PSNameChipBuild(UIElement pChild, double pPaddingRight) => new()
    {
        MinHeight = PSField.PSFieldChipHeight,
        BorderBrush = PSField.PSFieldLine,
        BorderThickness = new Thickness(1),
        Background = Brushes.White,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 0, pPaddingRight, 0),
        Margin = new Thickness(0, 0, 6, 6),
        Cursor = Cursors.Hand,
        Child = pChild
    };

    private static string PSNameOperatorFormat(string pKind, string pCount) =>
        $"{{{pKind}:{PSNameCountRead(pCount)}}}";

    private void PSNameChipAttach(Border pBorder, Func<string> pToken, UIElement? pCountBox)
    {
        var pGrip = new PSNameGrip();
        pBorder.PreviewMouseLeftButtonDown += (_, e) => PSNamePressHandle(pBorder, pGrip, pCountBox, e);
        pBorder.MouseEnter += (_, _) => PSNameEnterHandle(pBorder, pGrip);
        pBorder.MouseLeave += (_, _) => PSNameRestApply(pBorder);
        pBorder.MouseLeftButtonUp += (_, _) => PSNameReleaseHandle(pBorder, pGrip, pToken, pCountBox);
        pBorder.PreviewMouseMove += (_, e) => PSNameMoveHandle(pBorder, pGrip, pToken, e);
    }

    private static void PSNamePressHandle(Border pBorder, PSNameGrip pGrip, UIElement? pCountBox, MouseEventArgs e)
    {
        if (!PSNameGripSet(pBorder, pGrip, pCountBox, e))
        {
            return;
        }

        PSNamePressedApply(pBorder);
    }

    private static void PSNameEnterHandle(Border pBorder, PSNameGrip pGrip)
    {
        if (pGrip.PSNameGripActive)
        {
            return;
        }

        PSNameHoverApply(pBorder);
    }

    private void PSNameReleaseHandle(Border pBorder, PSNameGrip pGrip, Func<string> pToken, UIElement? pCountBox)
    {
        PSNameHoverApply(pBorder);
        if (!PSNameGripRelease(pGrip, pCountBox))
        {
            return;
        }

        PSNameTokenInsert(pToken());
    }

    private void PSNameMoveHandle(Border pBorder, PSNameGrip pGrip, Func<string> pToken, MouseEventArgs e)
    {
        if (!PSNameGripMove(pGrip, e))
        {
            return;
        }

        PSEncoderNameDrag.PSNameDragRun(
            psEncoderWindow.Content as UIElement, pBorder, pToken(), pGrip.PSNameGripOffset);
        PSNameRestApply(pBorder);
    }

    private static bool PSNameGripSet(Border pBorder, PSNameGrip pGrip, UIElement? pCountBox, MouseEventArgs e)
    {
        if (pCountBox is { IsMouseOver: true })
        {
            pGrip.PSNameGripOrigin = null;
            return false;
        }

        pGrip.PSNameGripOrigin = e.GetPosition(null);
        pGrip.PSNameGripOffset = e.GetPosition(pBorder);
        pGrip.PSNameGripActive = false;
        return true;
    }

    private static bool PSNameGripRelease(PSNameGrip pGrip, UIElement? pCountBox)
    {
        if (pGrip.PSNameGripActive)
        {
            pGrip.PSNameGripActive = false;
            return false;
        }

        return pCountBox is not { IsMouseOver: true };
    }

    private static bool PSNameGripMove(PSNameGrip pGrip, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || pGrip.PSNameGripOrigin is not Point pOrigin
            || pGrip.PSNameGripActive)
        {
            return false;
        }

        Point pCurrent = e.GetPosition(null);
        if (Math.Abs(pCurrent.X - pOrigin.X) < 4 && Math.Abs(pCurrent.Y - pOrigin.Y) < 4)
        {
            return false;
        }

        pGrip.PSNameGripActive = true;
        return true;
    }

    private static void PSNamePressedApply(Border pBorder) =>
        pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));

    private static void PSNameHoverApply(Border pBorder) =>
        pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

    private static void PSNameRestApply(Border pBorder) => pBorder.Background = Brushes.White;

    private sealed class PSNameGrip
    {
        internal Point? PSNameGripOrigin;
        internal Point PSNameGripOffset;
        internal bool PSNameGripActive;
    }

    private static bool PSNameDigitsCheck(string pText)
    {
        foreach (char pChar in pText)
        {
            if (!char.IsDigit(pChar))
            {
                return false;
            }
        }

        return true;
    }

    private static int PSNameCountRead(string pText)
    {
        return int.TryParse(pText, out int pCount) && pCount > 0 ? pCount : 1;
    }

    private void PSNameTokenInsert(string pToken)
    {
        QTokenControl.QTokenControlInsert(psNameBox, pToken);
    }
}
