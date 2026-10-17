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

        string lSheet = QButtonStyleApply(lButton, lLook);
        QButtonPartAttach(lButton);
        QLook.QLookAttach(lButton, lSheet);

        if (QButtonRecordCheck(lLook))
        {
            QInteractionClick.QInteractionClickAttach(lButton);
        }
    }

    // The Chrome and Exit families stay unrecorded; every other look records its clicks.
    private static bool QButtonRecordCheck(QButtonLook lLook) =>
        lLook is not (QButtonLook.QButtonLookChrome or QButtonLook.QButtonLookFirst
            or QButtonLook.QButtonLookExit or QButtonLook.QButtonLookLast);

    private static string QButtonStyleApply(Button lButton, QButtonLook lLook)
    {
        (Style lStyle, string lSheet) = lLook switch
        {
            QButtonLook.QButtonLookSource =>
                (QContract.QContractSheetFind<Style>(QButtonSourceStyle), QButtonSourceStyle),
            QButtonLook.QButtonLookCommand =>
                (QContract.QContractSheetFind<Style>(QButtonCommandStyle), QButtonCommandStyle),
            QButtonLook.QButtonLookFaded =>
                (QContract.QContractSheetFind<Style>(QButtonFadedStyle), QButtonFadedStyle),
            QButtonLook.QButtonLookPanel =>
                (QContract.QContractSheetFind<Style>(QButtonPanelStyle), QButtonPanelStyle),
            QButtonLook.QButtonLookWhite =>
                (QContract.QContractSheetFind<Style>(QButtonWhiteStyle), QButtonWhiteStyle),
            QButtonLook.QButtonLookIcon =>
                (QContract.QContractSheetFind<Style>(QButtonIconStyle), QButtonWhiteStyle),
            QButtonLook.QButtonLookChip =>
                (QContract.QContractSheetFind<Style>(QButtonChipStyle), QButtonWhiteStyle),
            QButtonLook.QButtonLookChrome =>
                (QContract.QContractSheetFind<Style>(QButtonChromeStyle), QButtonChromeStyle),
            QButtonLook.QButtonLookFirst =>
                (QContract.QContractSheetFind<Style>(QButtonFirstStyle), QButtonChromeStyle),
            QButtonLook.QButtonLookExit =>
                (QContract.QContractSheetFind<Style>(QButtonExitStyle), QButtonExitStyle),
            QButtonLook.QButtonLookLast =>
                (QContract.QContractSheetFind<Style>(QButtonLastStyle), QButtonExitStyle),
            _ => throw new ArgumentOutOfRangeException(nameof(lLook)),
        };

        lButton.Style = lStyle;
        return lSheet;
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
