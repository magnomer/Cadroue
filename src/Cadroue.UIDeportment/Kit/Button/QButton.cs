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

    // Every look's style key; a button whose style chain holds one gets its parts bound and its clicks recorded.
    private static readonly string[] QButtonStyles =
    [
        QButtonSourceStyle,
        QButtonCommandStyle,
        QButtonFadedStyle,
        QButtonPanelStyle,
        QButtonWhiteStyle,
        QButtonIconStyle,
        QButtonChipStyle,
        QButtonChromeStyle,
        QButtonFirstStyle,
        QButtonExitStyle,
        QButtonLastStyle,
    ];

    internal static void QButtonHook() =>
        EventManager.RegisterClassHandler(
            typeof(Button),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QButtonLoad((Button)lSender)),
            true);

    public static void QButtonAttach(Button lButton, QButtonLook lLook)
    {
        ArgumentNullException.ThrowIfNull(lButton);

        lButton.Style = QButtonStyleResolve(lLook);
        QButtonLoad(lButton);
        QLook.QLookAttach(lButton);
    }

    private static void QButtonLoad(Button lButton)
    {
        if (QLook.QLookStyleResolve(lButton, QButtonStyles) is not string lStyle)
        {
            return;
        }

        QButtonPartAttach(lButton);
        if (QButtonRecordCheck(lStyle))
        {
            QInteractionClick.QInteractionClickAttach(lButton);
        }
    }

    // The Chrome and Exit families stay unrecorded; every other look records its clicks.
    private static bool QButtonRecordCheck(string lStyle) =>
        lStyle is not (QButtonChromeStyle or QButtonFirstStyle or QButtonExitStyle or QButtonLastStyle);

    // A look picks its template style; the sheets its states follow come from the style's BasedOn chain.
    private static Style QButtonStyleResolve(QButtonLook lLook) =>
        lLook switch
        {
            QButtonLook.QButtonLookSource => QContract.QContractSheetFind<Style>(QButtonSourceStyle),
            QButtonLook.QButtonLookCommand => QContract.QContractSheetFind<Style>(QButtonCommandStyle),
            QButtonLook.QButtonLookFaded => QContract.QContractSheetFind<Style>(QButtonFadedStyle),
            QButtonLook.QButtonLookPanel => QContract.QContractSheetFind<Style>(QButtonPanelStyle),
            QButtonLook.QButtonLookWhite => QContract.QContractSheetFind<Style>(QButtonWhiteStyle),
            QButtonLook.QButtonLookIcon => QContract.QContractSheetFind<Style>(QButtonIconStyle),
            QButtonLook.QButtonLookChip => QContract.QContractSheetFind<Style>(QButtonChipStyle),
            QButtonLook.QButtonLookChrome => QContract.QContractSheetFind<Style>(QButtonChromeStyle),
            QButtonLook.QButtonLookFirst => QContract.QContractSheetFind<Style>(QButtonFirstStyle),
            QButtonLook.QButtonLookExit => QContract.QContractSheetFind<Style>(QButtonExitStyle),
            QButtonLook.QButtonLookLast => QContract.QContractSheetFind<Style>(QButtonLastStyle),
            _ => throw new ArgumentOutOfRangeException(nameof(lLook)),
        };

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
