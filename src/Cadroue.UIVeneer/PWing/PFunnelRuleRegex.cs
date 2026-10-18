using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelRuleRegex
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private const double PFunnelFieldHeight = 30;

    private readonly QFunnelCriterion lFunnelCriterion;
    private readonly QFunnelRule lFunnelRule;
    private readonly TextBox pFunnelRegexField;
    private readonly CheckBox pFunnelWholeBox;

    public PFunnelRuleRegex(QFunnelCriterion lCriterion, QFunnelRule lRule)
    {
        lFunnelCriterion = lCriterion;
        lFunnelRule = lRule;
        pFunnelRegexField = PFunnelRegexBuild();
        pFunnelWholeBox = PFunnelWholeBuild();
        PFunnelRegexStack = PFunnelStackBuild(lRule.QFunnelRulePattern);
    }

    public StackPanel PFunnelRegexStack { get; }

    public void PFunnelRegexShow()
    {
        pFunnelRegexField.Text = lFunnelCriterion.QFunnelRegexResolve(lFunnelRule, pFunnelRegexField.Text);
        pFunnelWholeBox.IsChecked = lFunnelRule.QFunnelRuleWhole;
    }

    private StackPanel PFunnelStackBuild(bool pPattern)
    {
        var pStack = new StackPanel
        {
            Visibility = PLook.PLookVisible[pPattern]
        };
        pStack.Children.Add(pFunnelRegexField);
        pStack.Children.Add(pFunnelWholeBox);
        return pStack;
    }

    private TextBox PFunnelRegexBuild()
    {
        TextBox pField = PFunnelRegexCreate();
        QTextbox.QTextboxAttach(pField);
        PFunnelRegexAttach(pField);
        return pField;
    }

    private static TextBox PFunnelRegexCreate() => new()
    {
        Height = PFunnelFieldHeight,
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private void PFunnelRegexAttach(TextBox pField) =>
        pField.TextChanged += (_, _) => lFunnelCriterion.QFunnelRegexSet(lFunnelRule, pField.Text);

    private CheckBox PFunnelWholeBuild()
    {
        CheckBox pBox = PFunnelWholeCreate();
        QInteractionClick.QInteractionClickAttach(pBox);
        QCheckbox.QCheckboxAttach(pBox);
        PFunnelWholeAttach(pBox);
        return pBox;
    }

    private static CheckBox PFunnelWholeCreate() => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Whole"),
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        Foreground = pFunnelTitleBrush,
        Margin = new Thickness(2, 0, 0, 0)
    };

    private void PFunnelWholeAttach(CheckBox pBox)
    {
        pBox.Checked += (_, _) => lFunnelCriterion.QFunnelWholeSet(lFunnelRule, true);
        pBox.Unchecked += (_, _) => lFunnelCriterion.QFunnelWholeSet(lFunnelRule, false);
    }
}
