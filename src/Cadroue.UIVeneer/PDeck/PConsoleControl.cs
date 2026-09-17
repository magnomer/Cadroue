using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PSCasement;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole
{
    private CheckBox PConsoleAutoBuild()
    {
        var pAutoBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Console.AutoResume.Label"),
            FontSize = PConsoleStationSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 4, 0),
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Console.AutoResume.Tooltip")
        };
        PCheckbox.PCheckboxApply(pAutoBox);
        pAutoBox.Checked += PConsoleAutoHandle;
        pAutoBox.Unchecked += PConsoleAutoHandle;
        return pAutoBox;
    }

    private static ComboBox PConsoleComboBuild()
    {
        var pRelayCombo = new ComboBox
        {
            Width = 180,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        PDropdown.PDropdownEditableApply(pRelayCombo);
        return pRelayCombo;
    }

    private static Border PConsoleSeparatorBuild() => new()
    {
        Width = 1,
        Margin = new Thickness(6, 2, 12, 2),
        VerticalAlignment = VerticalAlignment.Stretch,
        Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7))
    };

    private static Button PConsoleInlineBuild(string pIconName)
    {
        return new Button
        {
            Content = new Image
            {
                Source = PIcon.PIconRead($"/PAsset/PPanel/{pIconName}", PRosterTheme.PRosterTextBrush),
                Width = PConsoleSwitchSize,
                Height = PConsoleSwitchSize,
                Stretch = Stretch.Uniform
            },
            Width = 34,
            Height = PSField.PSFieldControlHeight,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
    }

    private static Button PConsoleSwitchBuild(string pIconName, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Source = PIcon.PIconRead($"/PAsset/PPanel/{pIconName}", PRosterTheme.PRosterTextBrush),
                Width = PConsoleSwitchSize,
                Height = PConsoleSwitchSize,
                Stretch = Stretch.Uniform
            },
            Width = PConsoleSwitchWidth,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            Style = PConsoleButtonCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Button PConsoleButtonBuild(
        string pLabel,
        string pIconName,
        string pTooltip,
        Brush? pAccentBrush,
        RoutedEventHandler pClick)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead(
                $"/PAsset/PPanel/{pIconName}",
                pAccentBrush ?? PRosterTheme.PRosterTextBrush),
            Width = PRosterTheme.PRosterIconSize,
            Height = PRosterTheme.PRosterIconSize,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 2 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = PRosterTheme.PRosterRowSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        var pButton = new Button
        {
            Width = PRosterTheme.PRosterButtonSize,
            Height = PRosterTheme.PRosterButtonSize,
            Margin = new Thickness(0, 0, 4, 0),
            Content = pStack,
            Style = PConsoleButtonCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Style PConsoleButtonCreate()
    {
        Style pStyle = PButton.PButtonCommandCreate();
        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, PRosterTheme.PRosterDisabledOpacity));
        pStyle.Triggers.Add(pDisabled);
        return pStyle;
    }

    private static TextBlock PConsoleLabelBuild(Brush pBrush, double pFontSize) => new()
    {
        FontSize = pFontSize,
        Foreground = pBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };
}
