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

    public static void QTokenControlAttach(RichTextBox lBox, QToken lToken, string lText)
    {
        ArgumentNullException.ThrowIfNull(lBox);
        ArgumentNullException.ThrowIfNull(lToken);

        QTokenStyleApply(lBox);
        QTokenHostBuild(lBox);
        QLook.QLookAttach(lBox, QTokenControlStyle);
        Paragraph lParagraph = QTokenParagraphBuild(lBox, lText);
        QTokenCaretSet(lBox, lParagraph.ContentEnd);
        QTokenHandlersAttach(lBox, lToken);
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
