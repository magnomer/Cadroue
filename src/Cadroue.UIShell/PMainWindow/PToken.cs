using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

internal sealed class PToken : RichTextBox
{
    internal const string PTokenDataFormat = "Cadroue.ExportNameToken";

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PTokenHoverBrush = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly Brush PTokenPressedBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));

    private readonly Paragraph pParagraph = new();
    private bool pTokenRenderActive;

    internal PToken()
    {
        Background = Brushes.White;
        Foreground = PTextBrush;
        BorderBrush = PLineBrush;
        BorderThickness = new Thickness(1);
        FontSize = 14;
        Padding = new Thickness(4, 0, 10, 0);
        VerticalContentAlignment = VerticalAlignment.Center;
        SelectionBrush = PAccentBrush;
        FocusVisualStyle = null;
        AcceptsReturn = false;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        Document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            FontFamily = FontFamily,
            FontSize = FontSize,
            Foreground = Foreground
        };
        pParagraph.Margin = new Thickness(0);
        pParagraph.Padding = new Thickness(0);
        pParagraph.LineHeight = 30;
        pParagraph.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        Document.Blocks.Add(pParagraph);
        Template = PTokenTemplateBuild();
        PreviewDragOver += PTokenDragHandle;
        PreviewDrop += PTokenDropHandle;
        TextChanged += PTokenTextHandle;
    }

    internal string Text
    {
        get => PTokenTextRead();
        set => PTokenTextSet(value);
    }

    internal void PTokenInsert(string pToken)
    {
        if (!Selection.IsEmpty)
        {
            Selection.Text = string.Empty;
        }

        var pContainer = PTokenInlineBuild(PTokenLabelRead(pToken), pToken, CaretPosition);
        CaretPosition = pContainer.ElementEnd.GetInsertionPosition(LogicalDirection.Forward) ?? pContainer.ElementEnd;
        Focus();
    }

    private void PTokenTextSet(string pText)
    {
        pTokenRenderActive = true;
        pParagraph.Inlines.Clear();

        foreach (object pPart in PTokenParse(pText))
        {
            if (pPart is string pRunText)
            {
                pParagraph.Inlines.Add(PTokenRunBuild(pRunText));
                continue;
            }

            if (pPart is Tuple<string, string> pToken)
            {
                pParagraph.Inlines.Add(PTokenInlineBuild(pToken.Item1, pToken.Item2));
            }
        }

        CaretPosition = pParagraph.ContentEnd;
        pTokenRenderActive = false;
    }

    private string PTokenTextRead()
    {
        var pText = new System.Text.StringBuilder();
        foreach (Inline pInline in pParagraph.Inlines)
        {
            if (pInline is Run pRun)
            {
                pText.Append(pRun.Text);
            }
            else if (pInline is InlineUIContainer pContainer && pContainer.Child is FrameworkElement pElement && pElement.Tag is string pToken)
            {
                pText.Append(pToken);
            }
        }

        return pText.ToString().TrimEnd('\r', '\n');
    }

    private static IEnumerable<object> PTokenParse(string pText)
    {
        int pIndex = 0;
        while (pIndex < pText.Length)
        {
            int pStart = pText.IndexOf('{', pIndex);
            if (pStart < 0)
            {
                yield return pText[pIndex..];
                yield break;
            }

            if (pStart > pIndex)
            {
                yield return pText[pIndex..pStart];
            }

            int pEnd = pText.IndexOf('}', pStart + 1);
            if (pEnd < 0)
            {
                yield return pText[pStart..];
                yield break;
            }

            string pToken = pText[pStart..(pEnd + 1)];
            yield return Tuple.Create(PTokenLabelRead(pToken), pToken);
            pIndex = pEnd + 1;
        }
    }

    private static string PTokenLabelRead(string pToken) => pToken switch
    {
        "{OriginalName}" => "Original Name",
        "{SectionNumber}" => "Section Number",
        "{Date}" => "Date",
        "{Time}" => "Time",
        _ => pToken.Trim('{', '}')
    };

    private static Run PTokenRunBuild(string pText)
    {
        return new Run(pText)
        {
            FontSize = 14,
            Foreground = PTextBrush,
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private static InlineUIContainer PTokenInlineBuild(string pLabel, string pToken, TextPointer? pPosition = null)
    {
        var pInline = pPosition is null
            ? new InlineUIContainer(PTokenChipBuild(pLabel, pToken))
            : new InlineUIContainer(PTokenChipBuild(pLabel, pToken), pPosition);
        pInline.BaselineAlignment = BaselineAlignment.Center;
        return pInline;
    }

    private static Border PTokenChipBuild(string pLabel, string pToken)
    {
        var pText = new TextBlock
        {
            Text = pLabel,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PTextBrush,
            LineHeight = 14,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pTextHost = new Grid();
        pTextHost.Children.Add(pText);

        var pChip = new Border
        {
            Tag = pToken,
            Height = 30,
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

    private void PTokenDragHandle(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(PTokenDataFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void PTokenDropHandle(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(PTokenDataFormat) is not string pToken)
        {
            return;
        }

        CaretPosition = GetPositionFromPoint(e.GetPosition(this), true) ?? CaretPosition;
        PTokenInsert(pToken);
        e.Handled = true;
    }

    private void PTokenTextHandle(object sender, TextChangedEventArgs e)
    {
        if (pTokenRenderActive)
        {
            return;
        }
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
        pFocusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pFocusTrigger);
        return pTemplate;
    }
}
