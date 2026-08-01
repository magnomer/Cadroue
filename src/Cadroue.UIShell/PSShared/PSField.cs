using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSField
{
    internal const double PSFieldFontSize = 12;
    internal const double PSFieldControlHeight = 32;
    internal const double PSFieldLabelWidth = 130;
    internal const double PSFieldChipHeight = 26;

    internal static readonly Brush PSFieldLine = PSFieldBrushCreate(0xD9, 0xDE, 0xE7);
    internal static readonly Brush PSFieldText = PSFieldBrushCreate(0x1D, 0x2A, 0x3D);
    internal static readonly Brush PSFieldMuted = PSFieldBrushCreate(0x62, 0x6F, 0x83);

    private static Brush PSFieldBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    internal static TextBlock PSFieldLabelBuild(string pText) => new()
    {
        Text = pText,
        Foreground = PSFieldMuted,
        VerticalAlignment = VerticalAlignment.Center
    };

    internal static UIElement PSPlateBuild(UIElement pContent) =>
        new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 18),
            Children = { pContent }
        };

    internal static UIElement PSPlateBuild(string pTitle, params UIElement[] pRows)
    {
        var pPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        pPanel.Children.Add(new TextBlock
        {
            Text = pTitle,
            Foreground = PSFieldText,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (UIElement pRow in pRows)
        {
            pPanel.Children.Add(pRow);
        }

        return pPanel;
    }

    internal static UIElement PSFieldBuild(string pLabel, Control pControl)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));
        pControl.MinHeight = PSFieldControlHeight;
        Grid.SetColumn(pControl, 1);
        pGrid.Children.Add(pControl);
        return pGrid;
    }

    internal static UIElement PSFieldBuild(string pLabel, UIElement pContent)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));
        Grid.SetColumn(pContent, 1);
        pGrid.Children.Add(pContent);
        return pGrid;
    }

    internal static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params Button[] pButtons)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pControl.MinHeight = PSFieldControlHeight;
        pPanel.Children.Add(pControl);
        foreach (Button pButton in pButtons)
        {
            pPanel.Children.Add(pButton);
        }

        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    internal static Button PSInlineButtonBuild(string pText, double pWidth, Thickness pMargin) => new()
    {
        Content = pText,
        Width = pWidth,
        Height = PSFieldControlHeight,
        Margin = pMargin,
        Style = PButton.PButtonWhiteCreate()
    };

    internal static Button PSInlineIconBuild(string pIconPath, string pTooltip, Thickness pMargin) => new()
    {
        Content = new Image
        {
            Source = PAssets.PIcon.PIconRead(pIconPath, PSFieldText),
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform
        },
        Width = 40,
        Height = PSFieldControlHeight,
        Padding = new Thickness(0),
        Margin = pMargin,
        ToolTip = pTooltip,
        Style = PButton.PButtonWhiteCreate()
    };

    internal static ComboBox PSComboBuild(string pSelected, params string[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.Contains(pSelected) ? pSelected : pItems.FirstOrDefault();
        return pCombo;
    }

    internal static ComboBox PSComboBuild(string pSelected, params LLocalizationChoice[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.FirstOrDefault(
            pItem => string.Equals(pItem.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
            ?? pItems.FirstOrDefault();
        return pCombo;
    }

    internal static TextBox PSEntryBuild(string pText, double pWidth)
    {
        var pTextBox = new TextBox
        {
            Text = pText,
            Width = pWidth,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PTextbox.PTextboxApply(pTextBox);
        pTextBox.Padding = new Thickness(6, 0, 10, 0);
        return pTextBox;
    }

    internal static Button PSFooterButtonBuild(string pText) => new()
    {
        Content = pText,
        Width = 84,
        Height = PSFieldControlHeight,
        Margin = new Thickness(4),
        Style = PButton.PButtonWhiteCreate()
    };

    internal static string PSComboTextRead(ComboBox pCombo) =>
        LLocalizationChoice.LLocalizationChoiceRead(pCombo.SelectedItem);

    private const double PSFieldSliderWidth = 220;
    private const double PSFieldBitrateTicks = 1000;

    internal static UIElement PSFieldSliderBuild(double pMinimum, double pMaximum, double pStep, string pValue, TextBox pReadout)
    {
        double pStart = double.TryParse(pValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double pParsed)
            ? Math.Clamp(pParsed, pMinimum, pMaximum)
            : pMinimum;

        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            SmallChange = pStep,
            LargeChange = pStep,
            TickFrequency = pStep,
            IsSnapToTickEnabled = true,
            Value = pStart,
            Width = PSFieldSliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);

        pReadout.IsReadOnly = true;
        pReadout.Text = PSFieldValueFormat(pStart, pStep);
        pSlider.ValueChanged += (_, _) => pReadout.Text = PSFieldValueFormat(pSlider.Value, pStep);
        return PSFieldSliderCompose(pSlider, pReadout);
    }

    internal static UIElement PSFieldBitrateBuild(double pMinimumKbps, double pMaximumKbps, string pValue, TextBox pReadout)
    {
        double pStartKbps = Math.Clamp(PSFieldBitrateParse(pValue) ?? pMinimumKbps, pMinimumKbps, pMaximumKbps);

        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = PSFieldBitrateTicks,
            SmallChange = 1,
            LargeChange = 50,
            Value = PSFieldPositionResolve(pStartKbps, pMinimumKbps, pMaximumKbps),
            Width = PSFieldSliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        pReadout.Text = PSFieldBitrateFormat(pStartKbps);

        bool pSync = false;
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            pSync = true;
            pReadout.Text = PSFieldBitrateFormat(PSFieldBitrateResolve(pSlider.Value, pMinimumKbps, pMaximumKbps));
            pSync = false;
        };
        pReadout.TextChanged += (_, _) =>
        {
            if (pSync || PSFieldBitrateParse(pReadout.Text) is not double pKbps)
            {
                return;
            }

            pSync = true;
            pSlider.Value = PSFieldPositionResolve(Math.Clamp(pKbps, pMinimumKbps, pMaximumKbps), pMinimumKbps, pMaximumKbps);
            pSync = false;
        };
        return PSFieldSliderCompose(pSlider, pReadout);
    }

    private static UIElement PSFieldSliderCompose(Slider pSlider, TextBox pReadout)
    {
        pReadout.Width = 68;
        pReadout.Margin = new Thickness(12, 0, 0, 0);
        pReadout.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pReadout);
        return pRow;
    }

    private static double PSFieldPositionResolve(double pKbps, double pMinimum, double pMaximum) =>
        Math.Log(pKbps / pMinimum) / Math.Log(pMaximum / pMinimum) * PSFieldBitrateTicks;

    private static double PSFieldBitrateResolve(double pPosition, double pMinimum, double pMaximum) =>
        pMinimum * Math.Pow(pMaximum / pMinimum, pPosition / PSFieldBitrateTicks);

    private static double? PSFieldBitrateParse(string pText)
    {
        pText = pText.Trim();
        if (pText.Length == 0)
        {
            return null;
        }

        char pUnit = pText[^1];
        string pNumber = char.IsDigit(pUnit) || pUnit == '.' ? pText : pText[..^1];
        if (!double.TryParse(pNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double pValue) || pValue <= 0)
        {
            return null;
        }

        return pUnit switch
        {
            'k' or 'K' => pValue,
            'm' or 'M' => pValue * 1000,
            _ => pValue / 1000
        };
    }

    private static string PSFieldBitrateFormat(double pKbps)
    {
        double pRounded = Math.Round(pKbps);
        return pRounded >= 1000
            ? (pRounded / 1000).ToString("0.###", CultureInfo.InvariantCulture) + "M"
            : ((long)pRounded).ToString(CultureInfo.InvariantCulture) + "k";
    }

    private static string PSFieldValueFormat(double pValue, double pStep) =>
        pStep >= 1 && pValue == Math.Floor(pValue)
            ? ((long)Math.Round(pValue)).ToString(CultureInfo.InvariantCulture)
            : pValue.ToString("0.##", CultureInfo.InvariantCulture);

    internal const string PSFieldCustomToken = "Custom";

    internal static UIElement PSFieldCustomBuild(string pLabel, TextBox pBox)
    {
        UIElement pRow = PSFieldBuild(pLabel, pBox);
        pRow.Visibility = Visibility.Collapsed;
        return pRow;
    }

    internal static void PSFieldCustomToggle(ComboBox pCombo, UIElement? pRow)
    {
        if (pRow is null)
        {
            return;
        }

        pRow.Visibility = string.Equals(PSComboTextRead(pCombo), PSFieldCustomToken, StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    internal static string PSFieldCustomResolve(string pValue, params string[] pTokens) =>
        Array.IndexOf(pTokens, pValue) >= 0 ? pValue : PSFieldCustomToken;

    internal static string PSFieldCustomRead(ComboBox pCombo, TextBox pBox, string pFallback)
    {
        string pSelected = PSComboTextRead(pCombo);
        if (!string.Equals(pSelected, PSFieldCustomToken, StringComparison.Ordinal))
        {
            return pSelected;
        }

        string pCustom = pBox.Text.Trim();
        return string.IsNullOrEmpty(pCustom) ? pFallback : pCustom;
    }

    internal static Thickness PSNoticeMargin => new(PSFieldLabelWidth, -7, 0, 9);

    internal static UIElement PSNoticeBuild(string pText) => new TextBlock
    {
        Text = pText,
        Foreground = PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin
    };
}
