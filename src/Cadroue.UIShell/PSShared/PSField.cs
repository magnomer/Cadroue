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
    internal static readonly Brush PSFieldAccent = PSFieldBrushCreate(0x4C, 0x86, 0xF7);
    internal static readonly Brush PSFieldInactive = PSFieldBrushCreate(0xF0, 0xF3, 0xFA);
    private static readonly Brush PSFieldSoft = PSFieldBrushCreate(0xF7, 0xF9, 0xFC);

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

    internal static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params UIElement[] pTrailing)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pControl.MinHeight = PSFieldControlHeight;
        pPanel.Children.Add(pControl);
        foreach (UIElement pElement in pTrailing)
        {
            pPanel.Children.Add(pElement);
        }

        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    internal static ProgressBar PSFieldProgressBuild() =>
        new()
        {
            Minimum = 0,
            Maximum = 1,
            Width = 140,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = null,
            Background = null,
            BorderThickness = new Thickness(0),
            Template = PSFieldTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static ControlTemplate PSFieldTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type ProgressBar}"">
    <Border CornerRadius=""4"" Background=""#E4E9F0"" ClipToBounds=""True"">
        <Grid>
            <Rectangle x:Name=""PART_Track"" />
            <Border x:Name=""PART_Indicator""
                    HorizontalAlignment=""Left""
                    CornerRadius=""4""
                    Background=""#4C86F7"" />
        </Grid>
    </Border>
</ControlTemplate>";
        return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(pXaml);
    }

    internal const double PSFieldListHeight = 300;

    internal static ListBox PSListBuild(string pSelected, params LLocalizationChoice[] pItems)
    {
        var pList = new ListBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSFieldListHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderBrush = PSFieldLine,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Padding = new Thickness(3),
            ItemContainerStyle = PSListStyleCreate()
        };
        PScrollbar.PScrollbarApply(pList);
        pList.SelectedItem = pItems.FirstOrDefault(
            pItem => string.Equals(pItem.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
            ?? pItems.FirstOrDefault();
        return pList;
    }

    private static Style PSListStyleCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pBorder.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
        pBorder.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));

        var pStyle = new Style(typeof(ListBoxItem));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(ListBoxItem)) { VisualTree = pBorder }));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PSFieldText));
        pStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, PSFieldSoft));
        pStyle.Triggers.Add(pHover);

        var pSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        pSelected.Setters.Add(new Setter(Control.BackgroundProperty, PSFieldAccent));
        pSelected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        pStyle.Triggers.Add(pSelected);

        return pStyle;
    }

    internal static string PSModeTextRead(Border pMode) => (string)(pMode.Tag ?? string.Empty);

    internal static Border PSModeBuild(string pSelected, Action pChange, params LLocalizationChoice[] pChoices) =>
        PSModeBuild(pSelected, pChange, out _, pChoices);

    internal static Border PSModeBuild(string pSelected, Action pChange, out Action<string, bool> pEnableSet, params LLocalizationChoice[] pChoices)
    {
        var pDisabled = new HashSet<string>(StringComparer.Ordinal);
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
                bool pOff = pDisabled.Contains(pToken);
                bool pFirst = pStyleIndex == 0;
                bool pLast = pStyleIndex == pSegments.Count - 1;
                pSegment.CornerRadius = new CornerRadius(pFirst ? 5 : 0, pLast ? 5 : 0, pLast ? 5 : 0, pFirst ? 5 : 0);
                pSegment.Background = pActive ? PSFieldAccent : PSFieldInactive;
                pSegment.IsHitTestVisible = !pOff;
                pSegment.Cursor = pOff ? Cursors.Arrow : Cursors.Hand;
                pSegment.Opacity = pOff ? 0.45 : 1;
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

        pEnableSet = (pToken, pEnabled) =>
        {
            if (pEnabled)
            {
                pDisabled.Remove(pToken);
            }
            else
            {
                pDisabled.Add(pToken);
                if (string.Equals((string)pHost.Tag, pToken, StringComparison.Ordinal))
                {
                    pHost.Tag = pSegments
                        .Select(pEntry => pEntry.Token)
                        .FirstOrDefault(pOther => !pDisabled.Contains(pOther)) ?? pToken;
                }
            }

            PSModeStyleApply();
        };

        PSModeStyleApply();
        return pHost;
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

        pRow.Visibility = string.Equals(PSCombo.PSComboTextRead(pCombo), PSFieldCustomToken, StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    internal static string PSFieldCustomResolve(string pValue, params string[] pTokens) =>
        Array.IndexOf(pTokens, pValue) >= 0 ? pValue : PSFieldCustomToken;

    internal static string PSFieldCustomRead(ComboBox pCombo, TextBox pBox, string pFallback)
    {
        string pSelected = PSCombo.PSComboTextRead(pCombo);
        if (!string.Equals(pSelected, PSFieldCustomToken, StringComparison.Ordinal))
        {
            return pSelected;
        }

        string pCustom = pBox.Text.Trim();
        return string.IsNullOrEmpty(pCustom) ? pFallback : pCustom;
    }
}
