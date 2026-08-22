using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    internal static readonly Brush PSFieldAccent = PSFieldBrushCreate(0x4C, 0x86, 0xF7);

    internal static string PSModeTextRead(Border pMode) => (string)(pMode.Tag ?? string.Empty);

    internal static Border PSModeBuild(string pSelected, Action pChange, params LLocalizationChoice[] pChoices)
    {
        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        var pHost = new Border
        {
            BorderBrush = PSFieldLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = PSFieldControlHeight,
            Child = pStrip
        };
        pHost.Tag = pChoices.Any(pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
            ? pSelected
            : pChoices.FirstOrDefault()?.LLocalizationChoiceToken ?? string.Empty;

        var pSegments = new List<(string Token, Border Segment, TextBlock Text)>();

        void PSModeStyleApply()
        {
            for (int pStyleIndex = 0; pStyleIndex < pSegments.Count; pStyleIndex++)
            {
                (string pToken, Border pSegment, TextBlock pText) = pSegments[pStyleIndex];
                bool pActive = string.Equals(pToken, (string)pHost.Tag, StringComparison.Ordinal);
                bool pFirst = pStyleIndex == 0;
                bool pLast = pStyleIndex == pSegments.Count - 1;
                pSegment.CornerRadius = new CornerRadius(pFirst ? 5 : 0, pLast ? 5 : 0, pLast ? 5 : 0, pFirst ? 5 : 0);
                pSegment.Background = pActive ? PSFieldAccent : Brushes.Transparent;
                pText.Foreground = pActive ? Brushes.White : PSFieldText;
            }
        }

        for (int pIndex = 0; pIndex < pChoices.Length; pIndex++)
        {
            LLocalizationChoice pChoice = pChoices[pIndex];
            string pToken = pChoice.LLocalizationChoiceToken;
            var pText = new TextBlock
            {
                Text = pChoice.ToString(),
                FontSize = PSFieldFontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var pSegment = new Border
            {
                Child = pText,
                Padding = new Thickness(16, 0, 16, 0),
                Cursor = Cursors.Hand,
                BorderBrush = PSFieldLine,
                BorderThickness = new Thickness(pIndex == 0 ? 0 : 1, 0, 0, 0)
            };
            pSegment.MouseLeftButtonUp += (_, _) =>
            {
                if (string.Equals((string)pHost.Tag, pToken, StringComparison.Ordinal))
                {
                    return;
                }

                pHost.Tag = pToken;
                PSModeStyleApply();
                pChange();
            };
            pSegments.Add((pToken, pSegment, pText));
            pStrip.Children.Add(pSegment);
        }

        PSModeStyleApply();
        return pHost;
    }

    private const double PSFieldSliderWidth = 220;
    private const double PSFieldBitrateTicks = 1000;

    internal static UIElement PSFieldSliderBuild(double pMinimum, double pMaximum, double pStep, string pValue, TextBox pReadout, bool pHigherBetter)
    {
        double pStart = double.TryParse(pValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double pParsed)
            && double.IsFinite(pParsed)
            ? PSFieldQualityNormalize(pParsed, pMinimum, pMaximum, pStep)
            : pMinimum;

        double PSFieldQualityResolve(double pPosition) => pHigherBetter ? pPosition : pMinimum + pMaximum - pPosition;

        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            SmallChange = pStep,
            LargeChange = pStep,
            TickFrequency = pStep,
            IsSnapToTickEnabled = true,
            Value = PSFieldQualityResolve(pStart),
            Width = PSFieldSliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);

        pReadout.Text = PSFieldValueFormat(pStart, pStep);

        bool pSync = false;
        void PSFieldQualityCommit()
        {
            double pQuality;
            if (!double.TryParse(pReadout.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pTyped)
                || !double.IsFinite(pTyped))
            {
                pQuality = PSFieldQualityResolve(pSlider.Value);
            }
            else
            {
                pQuality = PSFieldQualityNormalize(pTyped, pMinimum, pMaximum, pStep);
            }

            pSync = true;
            pSlider.Value = PSFieldQualityResolve(pQuality);
            pReadout.Text = PSFieldValueFormat(pQuality, pStep);
            pReadout.CaretIndex = pReadout.Text.Length;
            pSync = false;
        }

        pSlider.ValueChanged += (_, _) =>
        {
            if (!pSync)
            {
                pReadout.Text = PSFieldValueFormat(PSFieldQualityResolve(pSlider.Value), pStep);
            }
        };
        pReadout.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSFieldQualityCommit();
                pEvent.Handled = true;
            }
        };
        pReadout.LostKeyboardFocus += (_, _) => PSFieldQualityCommit();
        return PSFieldRowBuild(pSlider, pReadout);
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
        return PSFieldRowBuild(pSlider, pReadout);
    }

    internal static UIElement PSFieldDetentBuild(IReadOnlyList<int> pRates, bool pSnap, double pMaximum, string pZeroLabel, string pValue, TextBox pReadout, UIElement? pNotice = null)
    {
        double pMax = Math.Max(pMaximum, 1);
        double pStart = int.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pParsed) && pParsed > 0
            ? PSFieldDetentResolve(pParsed, pRates, pSnap)
            : 0;

        var pTicks = new DoubleCollection { 0 };
        foreach (int pRate in pRates)
        {
            pTicks.Add(pRate);
        }

        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = pMax,
            SmallChange = 1,
            LargeChange = 1,
            Ticks = pTicks,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = pSnap,
            Value = Math.Min(pStart, pMax),
            Width = PSFieldSliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        pSlider.IsSnapToTickEnabled = true;
        pReadout.IsReadOnly = pSnap;
        pReadout.Text = PSFieldDetentFormat(pStart, pZeroLabel);

        void PSFieldDetentNotice(double pAt)
        {
            if (pNotice is not null)
            {
                pNotice.Visibility = pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        PSFieldDetentNotice(pStart);

        bool pSync = false;
        void PSFieldDetentCommit()
        {
            string pTyped = pReadout.Text.Trim();
            double pResolved;
            if (pTyped.Length == 0 || string.Equals(pTyped, pZeroLabel, StringComparison.OrdinalIgnoreCase))
            {
                pResolved = 0;
            }
            else if (int.TryParse(pTyped, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pHz) && pHz > 0)
            {
                pResolved = PSFieldDetentResolve(pHz, pRates, pSnap);
            }
            else
            {
                pResolved = pSlider.Value;
            }

            pSync = true;
            pSlider.Value = Math.Min(pResolved, pMax);
            pReadout.Text = PSFieldDetentFormat(pResolved, pZeroLabel);
            pReadout.CaretIndex = pReadout.Text.Length;
            pSync = false;
            PSFieldDetentNotice(pResolved);
        }

        pSlider.ValueChanged += (_, _) =>
        {
            if (!pSync)
            {
                pReadout.Text = PSFieldDetentFormat(pSlider.Value, pZeroLabel);
            }

            PSFieldDetentNotice(pSlider.Value);
        };
        pReadout.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSFieldDetentCommit();
                pEvent.Handled = true;
            }
        };
        pReadout.LostKeyboardFocus += (_, _) => PSFieldDetentCommit();
        return PSFieldRowBuild(pSlider, pReadout);
    }

    internal static UIElement PSFieldLayoutBuild(Slider pSlider, IReadOnlyList<string> pLabels, int pIndex, TextBox pReadout, UIElement? pNotice = null)
    {
        int pLast = Math.Max(pLabels.Count - 1, 0);
        int pStart = Math.Clamp(pIndex, 0, pLast);

        var pTicks = new DoubleCollection();
        for (int pTick = 0; pTick <= pLast; pTick++)
        {
            pTicks.Add(pTick);
        }

        pSlider.Minimum = 0;
        pSlider.Maximum = pLast;
        pSlider.SmallChange = 1;
        pSlider.LargeChange = 1;
        pSlider.Ticks = pTicks;
        pSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
        pSlider.IsSnapToTickEnabled = true;
        pSlider.Value = pStart;
        pSlider.Width = PSFieldSliderWidth;
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        PSlider.PSliderApply(pSlider);
        pSlider.IsSnapToTickEnabled = true;

        pReadout.IsReadOnly = true;
        pReadout.Text = pLabels.Count > 0 ? pLabels[pStart] : string.Empty;

        if (pNotice is not null)
        {
            pNotice.Visibility = pStart <= 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        pSlider.ValueChanged += (_, _) =>
        {
            int pAt = Math.Clamp((int)Math.Round(pSlider.Value), 0, pLast);
            pReadout.Text = pLabels.Count > 0 ? pLabels[pAt] : string.Empty;
            if (pNotice is not null)
            {
                pNotice.Visibility = pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        return PSFieldRowBuild(pSlider, pReadout);
    }

    private static double PSFieldDetentResolve(double pHz, IReadOnlyList<int> pRates, bool pSnap)
    {
        if (pHz <= 0 || !pSnap || pRates.Count == 0)
        {
            return pHz <= 0 ? 0 : pHz;
        }

        double pBest = pRates[0];
        double pBestGap = double.MaxValue;
        foreach (int pRate in pRates)
        {
            double pGap = Math.Abs(pRate - pHz);
            if (pGap < pBestGap)
            {
                pBestGap = pGap;
                pBest = pRate;
            }
        }

        return pBest;
    }

    private static string PSFieldDetentFormat(double pValue, string pZeroLabel) =>
        pValue <= 0 ? pZeroLabel : ((long)Math.Round(pValue)).ToString(CultureInfo.InvariantCulture);

    private static UIElement PSFieldRowBuild(Slider pSlider, TextBox pReadout)
    {
        pReadout.Width = 88;
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

    internal static Slider PSFieldSliderCreate(double pMinimum, double pMaximum, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = Math.Max(pMaximum, pMinimum),
            SmallChange = 1,
            LargeChange = 1,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Value = Math.Clamp(pValue, pMinimum, Math.Max(pMaximum, pMinimum)),
            Width = PSFieldSliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        return pSlider;
    }

    internal static UIElement PSFieldRowBuild(Slider pSlider, FrameworkElement pTrailing)
    {
        pTrailing.Margin = new Thickness(12, 0, 0, 0);
        pTrailing.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pTrailing);
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
            ? (pRounded / 1000).ToString("0.##", CultureInfo.InvariantCulture) + "M"
            : ((long)pRounded).ToString(CultureInfo.InvariantCulture) + "k";
    }

    private static string PSFieldValueFormat(double pValue, double pStep) =>
        pStep >= 1 && pValue == Math.Floor(pValue)
            ? ((long)Math.Round(pValue)).ToString(CultureInfo.InvariantCulture)
            : pValue.ToString("0.##", CultureInfo.InvariantCulture);

    private static double PSFieldQualityNormalize(double pValue, double pMinimum, double pMaximum, double pStep)
    {
        double pClamped = Math.Clamp(pValue, pMinimum, pMaximum);
        double pStepped = pMinimum + Math.Round((pClamped - pMinimum) / pStep, MidpointRounding.AwayFromZero) * pStep;
        return Math.Clamp(pStepped, pMinimum, pMaximum);
    }

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
