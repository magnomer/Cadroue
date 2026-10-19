using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Cadroue.UIDeportment;

public static class QTokenControl
{
    public const string QTokenControlKind = "Cadroue.ExportNameToken";

    private const string QTokenControlStyle = "PToken";

    private const string QTokenControlDocument = "PTokenDocument";

    private const string QTokenControlHost = "PART_ContentHost";

    // The token logic the box inserts and drops through; a box without one takes no inserts or drops.
    internal static readonly DependencyProperty QTokenLogicProperty = DependencyProperty.RegisterAttached(
        "QTokenLogic",
        typeof(QToken),
        typeof(QTokenControl),
        new FrameworkPropertyMetadata(null));

    // The name notation the box shows when it loads.
    internal static readonly DependencyProperty QTokenTextProperty = DependencyProperty.RegisterAttached(
        "QTokenText",
        typeof(string),
        typeof(QTokenControl),
        new FrameworkPropertyMetadata(string.Empty));

    internal static void QTokenControlHook() =>
        EventManager.RegisterClassHandler(
            typeof(RichTextBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QTokenControlLoad((RichTextBox)lSender)),
            true);

    public static void QTokenControlAttach(RichTextBox lBox, QToken lToken, string lText)
    {
        ArgumentNullException.ThrowIfNull(lBox);
        ArgumentNullException.ThrowIfNull(lToken);

        lBox.SetValue(QTokenLogicProperty, lToken);
        lBox.SetValue(QTokenTextProperty, lText);
        QTokenStyleApply(lBox);
        QTokenControlLoad(lBox);
        QLook.QLookAttach(lBox);
    }

    private static void QTokenControlLoad(RichTextBox lBox)
    {
        if (QLook.QLookStyleResolve(lBox, [QTokenControlStyle]) is null)
        {
            return;
        }

        QTokenHostBuild(lBox);
        Paragraph lParagraph = QTokenParagraphBuild(lBox, (string)lBox.GetValue(QTokenTextProperty));
        QTokenCaretSet(lBox, lParagraph.ContentEnd);
        if (lBox.GetValue(QTokenLogicProperty) is QToken lToken)
        {
            QTokenHandlersAttach(lBox, lToken);
        }
    }

    private static void QTokenStyleApply(RichTextBox lBox) =>
        lBox.Style = QContract.QContractSheetFind<Style>(QTokenControlStyle);

    private static void QTokenHostBuild(RichTextBox lBox)
    {
        lBox.Document = QContract.QContractSheetFind<FlowDocument>(QTokenControlDocument);
        _ = lBox.ApplyTemplate();
        _ = ((FrameworkElement)lBox.Template.FindName(QTokenControlHost, lBox)).SetBinding(
            FrameworkElement.MarginProperty,
            new Binding(nameof(Control.Padding)) { Source = lBox });
    }

    private static Paragraph QTokenParagraphBuild(RichTextBox lBox, string lText)
    {
        Paragraph lParagraph = (Paragraph)lBox.Document.Blocks.FirstBlock;
        lParagraph.Inlines.Clear();
        lParagraph.Inlines.AddRange(QTokenNotation.QTokenParse(lText).Select(QTokenInline.QTokenInlineBuild));
        return lParagraph;
    }

    private static void QTokenCaretSet(RichTextBox lBox, TextPointer lPosition) => lBox.CaretPosition = lPosition;

    private static void QTokenHandlersAttach(RichTextBox lBox, QToken lToken)
    {
        lToken.QTokenInserted += lInserted => QTokenControlInsert(lBox, lInserted);
        lBox.PreviewDragOver += QTokenHoverHandle;
        lBox.PreviewDrop += (_, lEvent) => QTokenDroppedHandle(lBox, lToken, lEvent);
    }

    private static void QTokenHoverHandle(object sender, DragEventArgs e)
    {
        e.Effects = QTokenEffectResolve(e.Data);
        e.Handled = true;
    }

    private static DragDropEffects QTokenEffectResolve(IDataObject lData) =>
        lData.GetDataPresent(QTokenControlKind) ? DragDropEffects.Copy : DragDropEffects.None;

    private static void QTokenDroppedHandle(RichTextBox lBox, QToken lToken, DragEventArgs e)
    {
        lBox.CaretPosition = lBox.GetPositionFromPoint(e.GetPosition(lBox), true)!;
        e.Handled = lToken.QTokenDropHandle(e.Data.GetData(QTokenControlKind) as string);
    }

    public static string QTokenControlRead(RichTextBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        return QTokenNotation.QTokenTextRead(
            (lBox.Document.Blocks.FirstBlock as Paragraph)?.Inlines.Select(QTokenInline.QTokenInlineRead) ?? []);
    }

    public static void QTokenControlInsert(RichTextBox lBox, string lToken)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        InlineUIContainer lContainer = QTokenChipBuild(lBox, lToken);
        QTokenCaretSet(lBox, lContainer.ElementEnd.GetInsertionPosition(LogicalDirection.Forward)!);
        _ = lBox.Focus();
    }

    private static InlineUIContainer QTokenChipBuild(RichTextBox lBox, string lToken)
    {
        lBox.Selection.Text = string.Empty;
        return QTokenInline.QTokenInlineBuild(QTokenNotation.QTokenPartResolve(lToken), lBox.CaretPosition);
    }
}
