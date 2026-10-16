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

        lBox.Style = QContract.QContractSheetFind<Style>(QTokenControlStyle);
        lBox.Document = QContract.QContractSheetFind<FlowDocument>(QTokenControlDocument);
        _ = lBox.ApplyTemplate();
        _ = ((FrameworkElement)lBox.Template.FindName(QTokenControlHost, lBox)).SetBinding(
            FrameworkElement.MarginProperty,
            new Binding(nameof(Control.Padding)) { Source = lBox });
        QLook.QLookAttach(lBox, QTokenControlStyle);

        Paragraph lParagraph = (Paragraph)lBox.Document.Blocks.FirstBlock;
        lParagraph.Inlines.Clear();
        lParagraph.Inlines.AddRange(QToken.QTokenParse(lText).Select(QTokenInline.QTokenInlineBuild));
        lBox.CaretPosition = lParagraph.ContentEnd;

        lToken.QTokenInserted += lInserted => QTokenControlInsert(lBox, lInserted);
        lBox.PreviewDragOver += (_, lEvent) =>
        {
            lEvent.Effects = lEvent.Data.GetDataPresent(QTokenControlKind)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            lEvent.Handled = true;
        };
        lBox.PreviewDrop += (_, lEvent) =>
        {
            lBox.CaretPosition = lBox.GetPositionFromPoint(lEvent.GetPosition(lBox), true)!;
            lEvent.Handled = lToken.QTokenDropHandle(lEvent.Data.GetData(QTokenControlKind) as string);
        };
    }

    public static string QTokenControlRead(RichTextBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        return QToken.QTokenTextRead(
            (lBox.Document.Blocks.FirstBlock as Paragraph)?.Inlines.Select(QTokenInline.QTokenInlineRead) ?? []);
    }

    public static void QTokenControlInsert(RichTextBox lBox, string lToken)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        lBox.Selection.Text = string.Empty;
        InlineUIContainer lContainer = QTokenInline.QTokenInlineBuild(
            QToken.QTokenPartResolve(lToken), lBox.CaretPosition);
        lBox.CaretPosition = lContainer.ElementEnd.GetInsertionPosition(LogicalDirection.Forward)!;
        _ = lBox.Focus();
    }
}
