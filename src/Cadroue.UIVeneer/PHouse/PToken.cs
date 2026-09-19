using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PHouse;

internal sealed class PToken : RichTextBox
{
    internal const string PTokenDataKind = "Cadroue.ExportNameToken";

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PTokenTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PTokenAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PTokenHoverBrush = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly Brush PTokenPressedBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));
    private static readonly Brush PTokenOperatorBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x3B, 0x3B));

    private static readonly IReadOnlyDictionary<LTokenKind, Func<LTokenPart, UIElement>> PTokenFaces =
        new Dictionary<LTokenKind, Func<LTokenPart, UIElement>>
        {
            [LTokenKind.LTokenChip] = PTokenLabelBuild,
            [LTokenKind.LTokenOperator] = PTokenOperatorBuild,
        };

    private static readonly IReadOnlyDictionary<LTokenKind, Func<LTokenPart, Inline>> PTokenInlines =
        new Dictionary<LTokenKind, Func<LTokenPart, Inline>>
        {
            [LTokenKind.LTokenPlain] = PTokenRunBuild,
            [LTokenKind.LTokenChip] = PTokenContainerBuild,
            [LTokenKind.LTokenOperator] = PTokenContainerBuild,
        };

    private readonly Paragraph pTokenParagraph = new();
    private readonly LToken lToken = new();

    internal PToken()
    {
        Background = Brushes.White;
        Foreground = PTokenTextBrush;
        BorderBrush = PLineBrush;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(4, 0, 10, 0);
        VerticalContentAlignment = VerticalAlignment.Center;
        SelectionBrush = PTokenAccentBrush;
        FocusVisualStyle = null;
        AcceptsReturn = false;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        Document = new FlowDocument
        {
            PagePadding = new Thickness(0)
        };
        pTokenParagraph.Margin = new Thickness(0);
        pTokenParagraph.Padding = new Thickness(0);
        pTokenParagraph.LineHeight = 22;
        pTokenParagraph.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        Document.Blocks.Add(pTokenParagraph);
        Template = PTokenTemplateBuild();
        lToken.LTokenInsert += PTokenInsert;
        PreviewDragOver += PTokenDragHandle;
        PreviewDrop += PTokenDropHandle;
    }

    internal string PTokenText
    {
        get => LToken.LTokenTextRead(pTokenParagraph.Inlines.Select(PTokenInlineRead));
        set => PTokenTextSet(value);
    }

    internal void PTokenInsert(string pToken)
    {
        Selection.Text = string.Empty;
        InlineUIContainer pContainer = PTokenInlineBuild(LToken.LTokenPartResolve(pToken), CaretPosition);
        CaretPosition = pContainer.ElementEnd.GetInsertionPosition(LogicalDirection.Forward)!;
        Focus();
    }

    private void PTokenTextSet(string pText)
    {
        pTokenParagraph.Inlines.Clear();
        pTokenParagraph.Inlines.AddRange(LToken.LTokenParse(pText).Select(PTokenPartBuild));
        CaretPosition = pTokenParagraph.ContentEnd;
    }

    private static (string? PTokenText, string? PTokenTag) PTokenInlineRead(Inline pInline) =>
        ((pInline as Run)?.Text, ((pInline as InlineUIContainer)?.Child as FrameworkElement)?.Tag as string);

    private static Inline PTokenPartBuild(LTokenPart lPart) => PTokenInlines[lPart.LTokenPartKind](lPart);

    private static Inline PTokenContainerBuild(LTokenPart lPart) => PTokenInlineBuild(lPart, null);

    private static Inline PTokenRunBuild(LTokenPart lPart)
    {
        return new Run(lPart.LTokenPartText)
        {
            Foreground = PTokenTextBrush,
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private static InlineUIContainer PTokenInlineBuild(LTokenPart lPart, TextPointer? pPosition)
    {
        return new InlineUIContainer(PTokenChipBuild(lPart), pPosition)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private static Border PTokenChipBuild(LTokenPart lPart)
    {
        var pTextHost = new Grid();
        pTextHost.Children.Add(PTokenFaces[lPart.LTokenPartKind](lPart));
        var pChip = new Border
        {
            Tag = lPart.LTokenPartText,
            Height = 24,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = pTextHost
        };
        pChip.MouseEnter += (_, _) => pChip.Background = PTokenHoverBrush;
        pChip.MouseLeave += (_, _) => pChip.Background = Brushes.White;
        pChip.PreviewMouseLeftButtonDown += (_, _) => pChip.Background = PTokenPressedBrush;
        pChip.PreviewMouseLeftButtonUp += (_, _) => pChip.Background = PTokenHoverBrush;
        return pChip;
    }

    private static UIElement PTokenLabelBuild(LTokenPart lPart) => new TextBlock
    {
        Text = lPart.LTokenPartLabel,
        Foreground = PTokenTextBrush,
        LineHeight = 14,
        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static UIElement PTokenOperatorBuild(LTokenPart lPart)
    {
        var pContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pContent.Children.Add(new Image
        {
            Source = PIcon.PIconRead(lPart.LTokenPartIcon, PTokenOperatorBrush),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        });
        pContent.Children.Add(new TextBlock
        {
            Text = lPart.LTokenPartCount,
            Foreground = PTokenOperatorBrush,
            LineHeight = 14,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0)
        });
        return pContent;
    }

    private void PTokenDragHandle(object sender, DragEventArgs e)
    {
        e.Effects = PLook.PLookCopyEffect[e.Data.GetDataPresent(PTokenDataKind)];
        e.Handled = true;
    }

    private void PTokenDropHandle(object sender, DragEventArgs e)
    {
        CaretPosition = GetPositionFromPoint(e.GetPosition(this), true)!;
        e.Handled = lToken.LTokenDropHandle(e.Data.GetData(PTokenDataKind) as string);
    }

    private static ControlTemplate PTokenTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(RichTextBox));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "OuterBorder";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        pBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

        var pGrid = new FrameworkElementFactory(typeof(Grid));
        var pContent = new FrameworkElementFactory(typeof(ScrollViewer));
        pContent.Name = "PART_ContentHost";
        pContent.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pGrid.AppendChild(pContent);
        pBorder.AppendChild(pGrid);
        pTemplate.VisualTree = pBorder;

        var pFocusTrigger = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PTokenAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pFocusTrigger);
        return pTemplate;
    }
}
