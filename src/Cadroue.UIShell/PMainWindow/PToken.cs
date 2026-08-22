using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PMainWindow;

internal sealed class PToken : RichTextBox
{
    internal const string PTokenDataKind = "Cadroue.ExportNameToken";

    private const string PTokenBackspaceIcon = "/PAssets/PPanels/PTokenBackspace.svg";
    private const string PTokenDeleteIcon = "/PAssets/PPanels/PTokenDelete.svg";

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PTokenTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PTokenAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PTokenHoverBrush = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly Brush PTokenPressedBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));
    private static readonly Brush PTokenOperatorBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x3B, 0x3B));

    private readonly Paragraph pTokenParagraph = new();
    private bool pTokenRenderActive;

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
        PreviewDragOver += PTokenDragHandle;
        PreviewDrop += PTokenDropHandle;
        TextChanged += PTokenTextHandle;
    }

    internal string PTokenText
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
        pTokenParagraph.Inlines.Clear();

        foreach (object pPart in PTokenParse(pText))
        {
            if (pPart is string pRunText)
            {
                pTokenParagraph.Inlines.Add(PTokenRunBuild(pRunText));
                continue;
            }

            if (pPart is Tuple<string, string> pToken)
            {
                pTokenParagraph.Inlines.Add(PTokenInlineBuild(pToken.Item1, pToken.Item2));
            }
        }

        CaretPosition = pTokenParagraph.ContentEnd;
        pTokenRenderActive = false;
    }

    private string PTokenTextRead()
    {
        var pText = new System.Text.StringBuilder();
        foreach (Inline pInline in pTokenParagraph.Inlines)
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

    private static string PTokenLabelRead(string pToken)
    {
        if (PTokenOperatorRead(pToken, out string pOperatorLabel))
        {
            return pOperatorLabel;
        }

        return pToken switch
        {
        "{Prefix}" => LLocalization.LLocalizationTextRead("Token.Prefix.Label"),
        "{OriginalName}" => LLocalization.LLocalizationTextRead("Token.OriginalName.Label"),
        "{SectionNumber}" => LLocalization.LLocalizationTextRead("Token.SectionNumber.Label"),
        "{SectionName}" => LLocalization.LLocalizationTextRead("Token.SectionName.Label"),
        "{Date}" => LLocalization.LLocalizationTextRead("Token.Date.Label"),
        "{Time}" => LLocalization.LLocalizationTextRead("Token.Time.Label"),
        "{Suffix}" => LLocalization.LLocalizationTextRead("Token.Suffix.Label"),
            _ => pToken.Trim('{', '}')
        };
    }

    private static bool PTokenOperatorRead(string pToken, out string pLabel)
    {
        pLabel = string.Empty;
        string pInner = pToken.Trim('{', '}');
        int pColon = pInner.IndexOf(':');
        string pName = pColon < 0 ? pInner : pInner[..pColon];
        string pCount = pColon < 0 ? "1" : pInner[(pColon + 1)..];
        if (pName.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
        {
            pLabel = LLocalization.LLocalizationTextRead("Token.Backspace.Label") + pCount;
            return true;
        }

        if (pName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            pLabel = LLocalization.LLocalizationTextRead("Token.Delete.Label") + pCount;
            return true;
        }

        return false;
    }

    private static Run PTokenRunBuild(string pText)
    {
        return new Run(pText)
        {
            Foreground = PTokenTextBrush,
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

    private static bool PTokenIconRead(string pToken, out string pIconPath, out string pCount)
    {
        pIconPath = string.Empty;
        pCount = "1";
        string pInner = pToken.Trim('{', '}');
        int pColon = pInner.IndexOf(':');
        string pName = pColon < 0 ? pInner : pInner[..pColon];
        pCount = pColon < 0 ? "1" : pInner[(pColon + 1)..];
        if (pName.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
        {
            pIconPath = PTokenBackspaceIcon;
            return true;
        }

        if (pName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            pIconPath = PTokenDeleteIcon;
            return true;
        }

        return false;
    }

    private static Border PTokenChipBuild(string pLabel, string pToken)
    {
        var pTextHost = new Grid();
        if (PTokenIconRead(pToken, out string pIconPath, out string pCount))
        {
            pTextHost.Children.Add(PTokenOperatorBuild(pIconPath, pCount));
        }
        else
        {
            pTextHost.Children.Add(new TextBlock
            {
                Text = pLabel,
                Foreground = PTokenTextBrush,
                LineHeight = 14,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        var pChip = new Border
        {
            Tag = pToken,
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

    private static UIElement PTokenOperatorBuild(string pIconPath, string pCount)
    {
        var pContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pContent.Children.Add(new Image
        {
            Source = PIcon.PIconRead(pIconPath, PTokenOperatorBrush),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        });
        pContent.Children.Add(new TextBlock
        {
            Text = pCount,
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
        e.Effects = e.Data.GetDataPresent(PTokenDataKind) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void PTokenDropHandle(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(PTokenDataKind) is not string pToken)
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
        pFocusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PTokenAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pFocusTrigger);
        return pTemplate;
    }
}
