using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static Border PSPlateBuild(string pTitle, UIElement pContent)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock { Text = pTitle, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = PTextBrush, Margin = new Thickness(0, 0, 0, 10) });
        pPanel.Children.Add(pContent);
        return new Border
        {
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = PSoftBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = pPanel
        };
    }

    private static UIElement PSFieldBuild(string pLabel, Control pControl)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pLabel, Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });
        pControl.MinHeight = 28;
        Grid.SetColumn(pControl, 1);
        pGrid.Children.Add(pControl);
        return pGrid;
    }

    private static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params Button[] pButtons)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pLabel, Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pControl.MinHeight = 28;
        pPanel.Children.Add(pControl);
        foreach (Button pButton in pButtons)
        {
            pPanel.Children.Add(pButton);
        }

        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private static Button PSInlineButtonBuild(string pText, double pWidth, Thickness pMargin) => new()
    {
        Content = pText,
        Width = pWidth,
        Height = 40,
        Margin = pMargin,
        Style = PButton.PButtonWhiteCreate()
    };

    private static ComboBox PSComboBuild(string pSelected, params string[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.Contains(pSelected) ? pSelected : pItems.FirstOrDefault();
        return pCombo;
    }

    private static TextBox PSEntryBuild(string pText, double pWidth)
    {
        var pTextBox = new TextBox
        {
            Text = pText,
            Width = pWidth,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PTextbox.PTextboxApply(pTextBox);
        return pTextBox;
    }

    private static Button PSFooterButtonBuild(string pText)
    {
        return new Button
        {
            Content = pText,
            Width = 84,
            Margin = new Thickness(4),
            Style = PButton.PButtonWhiteCreate()
        };
    }

    private static string PSComboTextRead(ComboBox pCombo) => pCombo.SelectedItem as string ?? string.Empty;

    private static UIElement PSNoticeBuild(string pText) => new Border
    {
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xBF, 0xD4, 0xF4)),
        BorderThickness = new Thickness(1),
        Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFF)),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10),
        Margin = new Thickness(130, 2, 0, 2),
        Child = new TextBlock { Text = pText, Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x55, 0x88)), TextWrapping = TextWrapping.Wrap }
    };
}
