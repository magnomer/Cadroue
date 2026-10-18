using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelCondition : Grid
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private const double PFunnelFieldHeight = 30;
    private const double PFunnelJoinWidth = 78;

    private readonly QFunnel lFunnel;
    private readonly QFunnelRule lFunnelRule;
    private readonly QFunnelKind lFunnelKind;
    private readonly TextBox pFunnelField;
    private readonly PFunnelConditionCase pFunnelCase;
    private readonly PFunnelConditionJoin pFunnelJoin;

    public PFunnelCondition(QFunnel lFunnelOwner, QFunnelRule lRule, QFunnelCondition lCondition)
    {
        lFunnel = lFunnelOwner;
        lFunnelRule = lRule;
        lFunnelKind = lCondition.QFunnelConditionKind;
        pFunnelField = PFunnelFieldBuild();
        pFunnelCase = new PFunnelConditionCase(lFunnelOwner.QFunnelCriterion, lRule, lFunnelKind);
        pFunnelJoin = new PFunnelConditionJoin(
            lFunnelOwner.QFunnelCriterion, lRule, lFunnelKind, lCondition.QFunnelConditionJoin);
        PFunnelLayoutBuild(lCondition.QFunnelConditionLabel);
        PFunnelConditionUpdate();
    }

    public void PFunnelConditionUpdate()
    {
        LSceneFunnelCondition lMatch = lFunnel.QFunnelCriterion.QFunnelMatchRead(lFunnelRule, lFunnelKind);
        PFunnelFieldShow();
        pFunnelCase.PFunnelCaseApply(lMatch.LSceneFunnelCase);
        pFunnelCase.PFunnelCaseShow(lMatch.LSceneFunnelCase);
        pFunnelJoin.PFunnelJoinApply(lMatch.LSceneFunnelJoin);
    }

    private void PFunnelLayoutBuild(string pLabelKey)
    {
        Margin = new Thickness(0, 0, 0, 8);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead(pLabelKey),
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            Margin = new Thickness(2, 0, 0, 3)
        };
        SetRow(pLabel, 0);
        Children.Add(pLabel);

        var pLine = new Grid();
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PFunnelJoinWidth) });
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        SetColumn(pFunnelJoin.PFunnelJoinBorder, 0);
        pLine.Children.Add(pFunnelJoin.PFunnelJoinBorder);
        SetColumn(pFunnelField, 1);
        pLine.Children.Add(pFunnelField);
        SetColumn(pFunnelCase.PFunnelCaseBorder, 2);
        pLine.Children.Add(pFunnelCase.PFunnelCaseBorder);

        SetRow(pLine, 1);
        Children.Add(pLine);
    }

    private void PFunnelFieldShow() =>
        pFunnelField.Text = lFunnel.QFunnelCriterion.QFunnelTextResolve(lFunnelRule, lFunnelKind, pFunnelField.Text);

    private TextBox PFunnelFieldBuild()
    {
        TextBox pField = PFunnelFieldCreate();
        QTextbox.QTextboxAttach(pField);
        PFunnelFieldAttach(pField);
        return pField;
    }

    private static TextBox PFunnelFieldCreate() => new()
    {
        Height = PFunnelFieldHeight,
        FontSize = 12,
        FontFamily = pFunnelFontFamily
    };

    private void PFunnelFieldAttach(TextBox pField) =>
        pField.TextChanged += (_, _) => lFunnel.QFunnelCriterion.QFunnelTextSet(lFunnelRule, lFunnelKind, pField.Text);
}
