using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

public static class PConsoleControl
{
    internal const double PConsoleStatusSize = 13;
    internal const double PConsoleStationSize = 12;
    internal const double PConsoleSwitchWidth = 34;
    internal const double PConsoleSwitchSize = 18;

    internal static CheckBox PConsoleAutoBuild(RoutedEventHandler pChange)
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
        pAutoBox.Checked += pChange;
        pAutoBox.Unchecked += pChange;
        return pAutoBox;
    }

    internal static ComboBox PConsoleComboBuild()
    {
        var pRelayCombo = new ComboBox
        {
            Width = 180,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        PDropdown.PDropdownActionApply(
            pRelayCombo,
            LLocalization.LLocalizationTextRead("Console.Scene.DeleteTooltip"));
        pRelayCombo.ToolTip = LLocalization.LLocalizationTextRead("Console.Scene.ComboTooltip");
        return pRelayCombo;
    }

    internal static Border PConsoleSeparatorBuild() => new()
    {
        Width = 1,
        Margin = new Thickness(6, 2, 12, 2),
        VerticalAlignment = VerticalAlignment.Stretch,
        Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7))
    };

    internal static Button PConsoleInlineBuild(string pIconName, string pTooltip, RoutedEventHandler pClick)
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
            Width = 34,
            Height = PSField.PSFieldControlHeight,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    internal static Button PConsoleSwitchBuild(string pIconName, string pTooltip, RoutedEventHandler pClick)
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

    internal static Button PConsoleButtonBuild(
        string pLabelKey,
        string pIconName,
        Brush pAccentBrush,
        RoutedEventHandler pClick)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAsset/PPanel/{pIconName}", pAccentBrush),
            Width = PRosterTheme.PRosterIconSize,
            Height = PRosterTheme.PRosterIconSize,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 2 });
        pStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead($"Console.Button.{pLabelKey}"),
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
            ToolTip = LLocalization.LLocalizationTextRead($"Console.Button.{pLabelKey}Tooltip")
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

    internal static TextBlock PConsoleLabelBuild(Brush pBrush, double pFontSize) => new()
    {
        FontSize = pFontSize,
        Foreground = pBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };
}
