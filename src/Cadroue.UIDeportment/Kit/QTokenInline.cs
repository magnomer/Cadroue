using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

internal static class QTokenInline
{
    private const string QTokenInlineStyle = "PTokenChip";

    private const string QTokenInlineSlot = "PTokenChipSlot";

    private const string QTokenInlineLabel = "PTokenChipLabel";

    private const string QTokenInlineOperator = "PTokenChipOperator";

    private const string QTokenInlineBrush = "PTokenOperator";

    private static readonly IReadOnlyDictionary<QTokenKind, Func<QTokenPart, UIElement>> QTokenInlineFaces =
        new Dictionary<QTokenKind, Func<QTokenPart, UIElement>>
        {
            [QTokenKind.QTokenChip] = lPart =>
            {
                TextBlock lLabel = QContract.QContractSheetFind<TextBlock>(QTokenInlineLabel);
                lLabel.Text = lPart.QTokenPartLabel;
                return lLabel;
            },
            [QTokenKind.QTokenOperator] = lPart =>
            {
                Panel lFace = QContract.QContractSheetFind<Panel>(QTokenInlineOperator);
                ((Image)lFace.Children[0]).Source = QIcon.QIconRead(
                    lPart.QTokenPartIcon, QContract.QContractSheetFind<Brush>(QTokenInlineBrush));
                ((TextBlock)lFace.Children[1]).Text = lPart.QTokenPartCount;
                return lFace;
            },
        };

    private static readonly IReadOnlyDictionary<QTokenKind, Func<QTokenPart, Inline>> QTokenInlineKinds =
        new Dictionary<QTokenKind, Func<QTokenPart, Inline>>
        {
            [QTokenKind.QTokenPlain] = lPart => new Run(lPart.QTokenPartText)
            {
                BaselineAlignment = BaselineAlignment.Center,
            },
            [QTokenKind.QTokenChip] = lPart => QTokenInlineBuild(lPart, null),
            [QTokenKind.QTokenOperator] = lPart => QTokenInlineBuild(lPart, null),
        };

    internal static Inline QTokenInlineBuild(QTokenPart lPart)
    {
        ArgumentNullException.ThrowIfNull(lPart);

        return QTokenInlineKinds[lPart.QTokenPartKind](lPart);
    }

    internal static InlineUIContainer QTokenInlineBuild(QTokenPart lPart, TextPointer? lPosition)
    {
        ArgumentNullException.ThrowIfNull(lPart);

        ContentControl lChip = QContract.QContractSheetFind<ContentControl>(QTokenInlineSlot);
        lChip.Tag = lPart.QTokenPartText;
        lChip.Content = QTokenInlineFaces[lPart.QTokenPartKind](lPart);
        QLook.QLookAttach(lChip, QTokenInlineStyle);
        return new InlineUIContainer(lChip, lPosition)
        {
            BaselineAlignment = BaselineAlignment.Center,
        };
    }

    internal static (string? QTokenText, string? QTokenTag) QTokenInlineRead(Inline lInline) =>
        ((lInline as Run)?.Text, ((lInline as InlineUIContainer)?.Child as FrameworkElement)?.Tag as string);
}
