using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public static class QButton
{
    private const string QButtonSourceStyle = "PButtonSource";

    private const string QButtonCommandStyle = "PButtonCommand";

    private const string QButtonFadedStyle = "PButtonFaded";

    private const string QButtonPanelStyle = "PButtonPanel";

    private const string QButtonWhiteStyle = "PButtonWhite";

    private const string QButtonIconStyle = "PButtonIcon";

    private const string QButtonChipStyle = "PButtonChip";

    private const string QButtonChromeStyle = "PButtonChrome";

    private const string QButtonFirstStyle = "PButtonChromeFirst";

    private const string QButtonExitStyle = "PButtonExit";

    private const string QButtonLastStyle = "PButtonExitLast";

    private const string QButtonFramePart = "PButtonFrame";

    private const string QButtonFacePart = "PButtonFace";

    public static void QButtonAttach(Button lButton, QButtonLook lLook)
    {
        ArgumentNullException.ThrowIfNull(lButton);

        (Style lStyle, string lSheet, bool lRecorded) = lLook switch
        {
            QButtonLook.QButtonLookSource =>
                (QContract.QContractSheetFind<Style>(QButtonSourceStyle), QButtonSourceStyle, true),
            QButtonLook.QButtonLookCommand =>
                (QContract.QContractSheetFind<Style>(QButtonCommandStyle), QButtonCommandStyle, true),
            QButtonLook.QButtonLookFaded =>
                (QContract.QContractSheetFind<Style>(QButtonFadedStyle), QButtonFadedStyle, true),
            QButtonLook.QButtonLookPanel =>
                (QContract.QContractSheetFind<Style>(QButtonPanelStyle), QButtonPanelStyle, true),
            QButtonLook.QButtonLookWhite =>
                (QContract.QContractSheetFind<Style>(QButtonWhiteStyle), QButtonWhiteStyle, true),
            QButtonLook.QButtonLookIcon =>
                (QContract.QContractSheetFind<Style>(QButtonIconStyle), QButtonWhiteStyle, true),
            QButtonLook.QButtonLookChip =>
                (QContract.QContractSheetFind<Style>(QButtonChipStyle), QButtonWhiteStyle, true),
            QButtonLook.QButtonLookChrome =>
                (QContract.QContractSheetFind<Style>(QButtonChromeStyle), QButtonChromeStyle, false),
            QButtonLook.QButtonLookFirst =>
                (QContract.QContractSheetFind<Style>(QButtonFirstStyle), QButtonChromeStyle, false),
            QButtonLook.QButtonLookExit =>
                (QContract.QContractSheetFind<Style>(QButtonExitStyle), QButtonExitStyle, false),
            QButtonLook.QButtonLookLast =>
                (QContract.QContractSheetFind<Style>(QButtonLastStyle), QButtonExitStyle, false),
            _ => throw new ArgumentOutOfRangeException(nameof(lLook)),
        };

        lButton.Style = lStyle;
        QButtonPartAttach(lButton);
        QLook.QLookAttach(lButton, lSheet);
        if (lRecorded)
        {
            QInteractionClick.QInteractionClickAttach(lButton);
        }
    }

    public static Button QButtonBuild(Button lButton, QButtonLook lLook)
    {
        QButtonAttach(lButton, lLook);
        return lButton;
    }

    public static void QButtonChipAttach(FrameworkElement lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        lHost.Resources[QButtonChipStyle] =
            new Style(typeof(Button), QContract.QContractSheetFind<Style>(QButtonChipStyle))
            {
                Setters = { new EventSetter(FrameworkElement.LoadedEvent, new RoutedEventHandler(QButtonChipLoad)) },
            };
    }

    private static void QButtonChipLoad(object lSender, RoutedEventArgs lEvent) =>
        QButtonAttach((Button)lSender, QButtonLook.QButtonLookChip);

    private static void QButtonPartAttach(Button lButton)
    {
        _ = lButton.ApplyTemplate();
        FrameworkElement lFrame = (FrameworkElement)lButton.Template.FindName(QButtonFramePart, lButton);
        (DependencyProperty, string)[] lSlots =
        [
            (Border.BackgroundProperty, nameof(Control.Background)),
            (Border.BorderBrushProperty, nameof(Control.BorderBrush)),
            (Border.BorderThicknessProperty, nameof(Control.BorderThickness)),
        ];
        foreach ((DependencyProperty lProperty, string lSource) in lSlots)
        {
            _ = lFrame.SetBinding(lProperty, new Binding(lSource) { Source = lButton });
        }

        _ = ((FrameworkElement)lButton.Template.FindName(QButtonFacePart, lButton)).SetBinding(
            FrameworkElement.MarginProperty, new Binding(nameof(Control.Padding)) { Source = lButton });
    }
}
