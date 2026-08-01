using Cadroue.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

internal enum PFunnelKind { Contains, Start, End, Extension }

internal sealed class PFunnelCondition : Grid
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly FontFamily pFunnelMonoFamily = new("Consolas");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private const double PFunnelFieldHeight = 30;
    private const double PFunnelJoinWidth = 78;
    private const double PFunnelCaseWidth = 38;

    private readonly PFunnelKind pFunnelKind;
    private readonly TextBox pFunnelField;
    private readonly Border pFunnelCaseButton;
    private readonly Border? pFunnelJoin;
    private bool pFunnelCase;
    private bool pFunnelAnd = true;

    public event Action? PFunnelConditionChange;

    public PFunnelCondition(PFunnelKind pKind, string pLabelKey, bool pHasJoin)
    {
        pFunnelKind = pKind;
        pFunnelField = PFunnelFieldBuild();
        pFunnelCaseButton = PFunnelCaseBuild();
        if (pHasJoin)
        {
            pFunnelJoin = PFunnelJoinBuild();
        }

        PFunnelCaseApply();
        if (pFunnelJoin is not null)
        {
            PFunnelJoinApply();
        }

        Margin = new Thickness(0, 0, 0, 8);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pLabelKey),
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

        if (pFunnelJoin is { } pJoin)
        {
            SetColumn(pJoin, 0);
            pLine.Children.Add(pJoin);
        }

        SetColumn(pFunnelField, 1);
        pLine.Children.Add(pFunnelField);

        SetColumn(pFunnelCaseButton, 2);
        pLine.Children.Add(pFunnelCaseButton);

        SetRow(pLine, 1);
        Children.Add(pLine);
    }

    public PFunnelKind PFunnelConditionKind => pFunnelKind;

    public string PFunnelConditionText => pFunnelField.Text.Trim();

    public bool PFunnelConditionAnd => pFunnelAnd;

    public bool PFunnelConditionMatch(string pFileName)
    {
        string pText = PFunnelConditionText;
        StringComparison pComparison = pFunnelCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return pFunnelKind switch
        {
            PFunnelKind.Contains => pFileName.Contains(pText, pComparison),
            PFunnelKind.Start => pFileName.StartsWith(pText, pComparison),
            PFunnelKind.End => pFileName.EndsWith(pText, pComparison),
            PFunnelKind.Extension => PFunnelExtensionMatch(pFileName, pText, pComparison),
            _ => false
        };
    }

    public LSceneFunnelMatch PFunnelConditionRecordRead()
    {
        return new LSceneFunnelMatch
        {
            LSceneFunnelText = PFunnelConditionText,
            LSceneFunnelCase = pFunnelCase,
            LSceneFunnelJoin = pFunnelAnd
        };
    }

    public void PFunnelConditionRestore(LSceneFunnelMatch? pMatch)
    {
        pMatch ??= new LSceneFunnelMatch();
        pFunnelField.Text = pMatch.LSceneFunnelText;
        pFunnelCase = pMatch.LSceneFunnelCase;
        pFunnelAnd = pMatch.LSceneFunnelJoin;
        PFunnelCaseApply();
        if (pFunnelJoin is not null)
        {
            PFunnelJoinApply();
        }
    }

    private static bool PFunnelExtensionMatch(string pFileName, string pText, StringComparison pComparison)
    {
        string pExtension = Path.GetExtension(pFileName).TrimStart('.');
        string pWanted = pText.TrimStart('.');
        return string.Equals(pExtension, pWanted, pComparison);
    }

    private static TextBox PFunnelFieldBuild()
    {
        var pField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily
        };
        PTextbox.PTextboxApply(pField);
        return pField;
    }

    private Border PFunnelCaseBuild()
    {
        var pLabel = new TextBlock
        {
            FontFamily = pFunnelMonoFamily,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pHost = new Border
        {
            Width = PFunnelCaseWidth,
            Height = PFunnelFieldHeight,
            Margin = new Thickness(6, 0, 0, 0),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            Cursor = Cursors.Hand,
            SnapsToDevicePixels = true,
            Child = pLabel
        };
        pHost.MouseLeftButtonUp += (_, _) => PFunnelCaseToggle();
        return pHost;
    }

    private void PFunnelCaseToggle()
    {
        pFunnelCase = !pFunnelCase;
        PFunnelCaseApply();
        PFunnelConditionChange?.Invoke();
    }

    private void PFunnelCaseApply()
    {
        bool pOn = pFunnelCase;
        pFunnelCaseButton.BorderBrush = pOn ? pFunnelAccentBrush : pFunnelLineBrush;
        pFunnelCaseButton.ToolTip = LLocalization.LLocalizationTextRead(
            pOn ? "Inspector.Funnel.CaseOn" : "Inspector.Funnel.CaseOff");

        if (pFunnelCaseButton.Child is TextBlock pLabel)
        {
            pLabel.Text = pOn ? "ABC" : "abc";
            pLabel.Foreground = pOn ? pFunnelAccentBrush : pFunnelMutedBrush;
        }
    }

    private Border PFunnelJoinBuild()
    {
        return new Border
        {
            Height = PFunnelFieldHeight,
            Margin = new Thickness(0, 0, 6, 0),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true
        };
    }

    private void PFunnelJoinApply()
    {
        if (pFunnelJoin is not { } pHost)
        {
            return;
        }

        bool pAnd = pFunnelAnd;
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());

        Border pAndSegment = PFunnelSegmentBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.And"), pAnd, () => PFunnelModeSet(true));
        var pDivider = new Border { Width = 1, Background = pFunnelLineBrush };
        Border pOrSegment = PFunnelSegmentBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Or"), !pAnd, () => PFunnelModeSet(false));

        Grid.SetColumn(pAndSegment, 0);
        Grid.SetColumn(pDivider, 1);
        Grid.SetColumn(pOrSegment, 2);
        pGrid.Children.Add(pAndSegment);
        pGrid.Children.Add(pDivider);
        pGrid.Children.Add(pOrSegment);
        pHost.Child = pGrid;
    }

    private static Border PFunnelSegmentBuild(string pText, bool pActive, Action pClick)
    {
        var pLabel = new TextBlock
        {
            Text = pText,
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            FontWeight = pActive ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = pActive ? pFunnelTitleBrush : pFunnelMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pSegment = new Border
        {
            Background = pActive ? pFunnelActiveBrush : Brushes.Transparent,
            Padding = new Thickness(4, 3, 4, 3),
            Cursor = Cursors.Hand,
            Child = pLabel
        };
        pSegment.MouseLeftButtonUp += (_, _) => pClick();
        return pSegment;
    }

    private void PFunnelModeSet(bool pAndMode)
    {
        if (pFunnelAnd == pAndMode)
        {
            return;
        }

        pFunnelAnd = pAndMode;
        PFunnelJoinApply();
        PFunnelConditionChange?.Invoke();
    }
}
