using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.ShellEngine;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PAction
{
    private static readonly Brush pActionRelayLine = new SolidColorBrush(Color.FromRgb(0xC9, 0xD6, 0xE5));
    private static readonly Brush pActionRelayHover = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFC));
    private static readonly Brush pActionHoverLine = new SolidColorBrush(Color.FromRgb(0xD5, 0xE0, 0xED));

    public void PActionRelayApply(Guid pActionRelayTarget)
    {
        PActionRelayTarget = pActionRelayTarget;
        PActionFaceUpdate();
    }

    private Button PActionRelayBuild()
    {
        var pChevron = new System.Windows.Shapes.Path
        {
            Stroke = pActionRelayText,
            StrokeThickness = 1.3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data = Geometry.Parse("M 3 4 L 6 7 L 9 4"),
            Width = 9,
            Height = 6,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };

        var pFace = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pActionRelayIcon, Dock.Left);
        DockPanel.SetDock(pChevron, Dock.Right);
        pFace.Children.Add(pActionRelayIcon);
        pFace.Children.Add(pChevron);
        pFace.Children.Add(pActionRelayLabel);

        var pButton = new Button
        {
            Height = 42,
            MinWidth = 142,
            MaxWidth = 190,
            Padding = new Thickness(11, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.White,
            BorderBrush = pActionRelayLine,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            Content = pFace,
            Template = PActionTemplateBuild(),
            ToolTip = LLocalization.LLocalizationTextRead("Action.Relay.Tooltip")
        };
        System.Windows.Automation.AutomationProperties.SetName(
            pButton, LLocalization.LLocalizationTextRead("Action.Relay.Name"));
        pButton.Click += PActionOpenHandle;
        PActionFaceUpdate();
        return pButton;
    }

    private static ControlTemplate PActionTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pFrame = new FrameworkElementFactory(typeof(Border));
        pFrame.Name = "pActionRelayFrame";
        pFrame.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pFrame.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        pFrame.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        pFrame.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        pFrame.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        pFrame.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pFrame.AppendChild(pContent);
        pTemplate.VisualTree = pFrame;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, pActionRelayHover, "pActionRelayFrame"));
        pHover.Setters.Add(new Setter(Border.BorderBrushProperty, pActionHoverLine, "pActionRelayFrame"));
        pTemplate.Triggers.Add(pHover);
        return pTemplate;
    }

    private static Image PActionIconBuild() => new()
    {
        Width = 17,
        Height = 17,
        Stretch = Stretch.Uniform,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Visibility = Visibility.Collapsed
    };

    private static TextBlock PActionLabelBuild() => new()
    {
        FontSize = 12,
        Foreground = pActionRelayText,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private void PActionFaceUpdate()
    {
        if (PActionRelayTarget == LCartographer.LCartographerFinishTarget)
        {
            pActionRelayLabel.Text = LLocalization.LLocalizationTextRead("Action.Relay.Finish");
            pActionRelayIcon.Source = null;
            pActionRelayIcon.Visibility = Visibility.Collapsed;
            return;
        }

        PActionRelayOption? pOption = PActionRelayTarget == Guid.Empty
            ? null
            : PActionOptionsRead().FirstOrDefault(pRow => pRow.PActionRelayId == PActionRelayTarget);

        if (PActionRelayTarget != Guid.Empty && pOption is null)
        {
            PActionRelayTarget = Guid.Empty;
        }

        pActionRelayLabel.Text = pOption?.PActionRelayTitle
            ?? LLocalization.LLocalizationTextRead("Action.Relay.None");
        pActionRelayIcon.Source = pOption?.PActionRelayIcon;
        pActionRelayIcon.Visibility = pOption?.PActionRelayIcon is null
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private IReadOnlyList<PActionRelayOption> PActionOptionsRead() =>
        PActionRelaySource?.Invoke() ?? Array.Empty<PActionRelayOption>();

    private void PActionOpenHandle(object pSender, RoutedEventArgs pArgs)
    {
        ContextMenu pRelayMenu = PPorch.PMenu.PMenuCreate(pActionRelayButton);
        PActionRowAppend(pRelayMenu, Guid.Empty, LLocalization.LLocalizationTextRead("Action.Relay.None"), null);
        foreach (PActionRelayOption pOption in PActionOptionsRead())
        {
            PActionRowAppend(pRelayMenu, pOption.PActionRelayId, pOption.PActionRelayTitle, pOption.PActionRelayIcon);
        }

        PActionRowAppend(
            pRelayMenu,
            LCartographer.LCartographerFinishTarget,
            LLocalization.LLocalizationTextRead("Action.Relay.Finish"),
            null);

        pRelayMenu.IsOpen = true;
        pArgs.Handled = true;
    }

    private void PActionRowAppend(
        ContextMenu pRelayMenu,
        Guid pRelayTarget,
        string pRelayTitle,
        ImageSource? pRelayIcon)
    {
        MenuItem pRelayItem = PPorch.PMenu.PMenuItemCreate(pRelayTitle, pRelayIcon);
        pRelayItem.Click += (_, _) => PActionRelaySelect(pRelayTarget);
        pRelayMenu.Items.Add(pRelayItem);
    }

    private void PActionRelaySelect(Guid pRelayTarget)
    {
        if (pRelayTarget == PActionRelayTarget)
        {
            return;
        }

        PActionRelayTarget = pRelayTarget;
        PActionFaceUpdate();
        PActionRelayChange?.Invoke(pRelayTarget);
    }
}
