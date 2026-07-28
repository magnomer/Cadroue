using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    internal const double PSSheetBodyFontSize = 12;

    internal const double PSSheetControlHeight = 32;

    private const double PSSheetChipHeight = 26;

    private static TextBlock PSSheetLabelBuild(string pText) => new()
    {
        Text = pText,
        Foreground = PMutedBrush,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static UIElement PSPlateBuild(UIElement pContent) =>
        new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 18),
            Children = { pContent }
        };

    private static UIElement PSFieldBuild(string pLabel, Control pControl)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSSheetLabelBuild(pLabel));
        pControl.MinHeight = PSSheetControlHeight;
        Grid.SetColumn(pControl, 1);
        pGrid.Children.Add(pControl);
        return pGrid;
    }

    private static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params Button[] pButtons)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSSheetLabelBuild(pLabel));

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pControl.MinHeight = PSSheetControlHeight;
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
        Height = PSSheetControlHeight,
        Margin = pMargin,
        Style = PButton.PButtonWhiteCreate()
    };

    private static ComboBox PSComboBuild(string pSelected, params string[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSSheetControlHeight,
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
            Height = PSSheetControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PTextbox.PTextboxApply(pTextBox);
        pTextBox.Padding = new Thickness(6, 0, 10, 0);
        return pTextBox;
    }

    private static Button PSFooterButtonBuild(string pText)
    {
        return new Button
        {
            Content = pText,
            Width = 84,
            Height = PSSheetControlHeight,
            Margin = new Thickness(4),
            Style = PButton.PButtonWhiteCreate()
        };
    }

    private static string PSComboTextRead(ComboBox pCombo) => pCombo.SelectedItem as string ?? string.Empty;

    private static Thickness PSSheetNoticeMargin => new(130, -7, 0, 9);

    private static UIElement PSNoticeBuild(string pText) => new TextBlock
    {
        Text = pText,
        Foreground = PMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSSheetNoticeMargin
    };
}
