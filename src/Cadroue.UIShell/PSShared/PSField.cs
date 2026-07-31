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

    internal static Thickness PSNoticeMargin => new(PSFieldLabelWidth, -7, 0, 9);

    internal static UIElement PSNoticeBuild(string pText) => new TextBlock
    {
        Text = pText,
        Foreground = PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin
    };
}
