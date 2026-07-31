using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAction : UserControl
{
    private static readonly Brush pActionPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pActionNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
    private static readonly Brush pActionRelayLine = new SolidColorBrush(Color.FromRgb(0xC9, 0xD6, 0xE5));
    private static readonly Brush pActionRelayText = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pActionRelayHover = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFC));
    private static readonly Brush pActionHoverLine = new SolidColorBrush(Color.FromRgb(0xD5, 0xE0, 0xED));
    private readonly Button pActionAllButton;
    private readonly Button pActionRelayButton;
    private readonly Image pActionRelayIcon;
    private readonly TextBlock pActionRelayLabel;

    public event Action<LWorkPriority>? PActionRun;
    public event Action? PActionAllAdd;
    public event Action<Guid>? PActionRelayChange;

    public PAction()
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Button pAddListButton = PActionButtonBuild("AddList", "PActionAddList.svg", "Action.AddList");
        pActionAllButton = PActionButtonBuild("AddAll", "PActionAddAll.svg", "Action.AddAll");
        Button pExecuteButton = PActionButtonBuild("Execute", "PActionExecute.svg", "Action.Execute");
        pAddListButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
        pActionAllButton.Click += (_, _) => PActionAllAdd?.Invoke();
        pExecuteButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        pActionAllButton.ToolTip = LLocalization.LLocalizationTextRead("Action.AddAll.Tooltip");
        pActionRelayIcon = PActionIconBuild();
        pActionRelayLabel = PActionLabelBuild();
        pActionRelayButton = PActionRelayBuild();
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pActionAllButton);
        pPanel.Children.Add(new Border { Width = 8 });
        pPanel.Children.Add(pActionRelayButton);
        pPanel.Children.Add(new Border { Width = 8 });
        pPanel.Children.Add(pExecuteButton);
        Content = new Border { Child = pPanel };
    }

    public Guid PActionRelayTarget { get; private set; }

    public Func<IReadOnlyList<LCourierOption>>? PActionRelaySource { get; set; }

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
        LCourierOption? pOption = PActionRelayTarget == Guid.Empty
            ? null
            : PActionOptionsRead().FirstOrDefault(pRow => pRow.LCourierTabId == PActionRelayTarget);

        if (PActionRelayTarget != Guid.Empty && pOption is null)
        {
            PActionRelayTarget = Guid.Empty;
        }

        pActionRelayLabel.Text = pOption?.LCourierTabTitle
            ?? LLocalization.LLocalizationTextRead("Action.Relay.None");
        pActionRelayIcon.Source = pOption?.LCourierTabIcon;
        pActionRelayIcon.Visibility = pOption?.LCourierTabIcon is null
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private IReadOnlyList<LCourierOption> PActionOptionsRead() =>
        PActionRelaySource?.Invoke() ?? Array.Empty<LCourierOption>();

    private void PActionOpenHandle(object pSender, RoutedEventArgs pArgs)
    {
        ContextMenu pRelayMenu = PControlBar.PMenu.PMenuCreate(pActionRelayButton);
        PActionRowAppend(pRelayMenu, Guid.Empty, LLocalization.LLocalizationTextRead("Action.Relay.None"), null);
        foreach (LCourierOption pOption in PActionOptionsRead())
        {
            PActionRowAppend(pRelayMenu, pOption.LCourierTabId, pOption.LCourierTabTitle, pOption.LCourierTabIcon);
        }

        pRelayMenu.IsOpen = true;
        pArgs.Handled = true;
    }

    private void PActionRowAppend(
        ContextMenu pRelayMenu,
        Guid pRelayTarget,
        string pRelayTitle,
        ImageSource? pRelayIcon)
    {
        MenuItem pRelayItem = PControlBar.PMenu.PMenuItemCreate(pRelayTitle, pRelayIcon);
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

    public void PActionAllSet(bool pActionAllAllowed, string pActionAllTooltip)
    {
        pActionAllButton.IsEnabled = pActionAllAllowed;
        pActionAllButton.Opacity = pActionAllAllowed ? 1 : 0.35;
        pActionAllButton.ToolTip = pActionAllTooltip;
    }

    private static Button PActionButtonBuild(string pActionToken, string pIconAssetName, string pLabelKey)
    {
        string pLabelText = LLocalization.LLocalizationTextRead(pLabelKey);
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAssets/PCompass/{pIconAssetName}", PActionAccentRead(pActionToken)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2F))
        });

        return new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PMainWindow.PButton.PButtonCommandCreate(),
            ToolTip = pLabelText
        };
    }

    private static Brush? PActionAccentRead(string pActionToken) => pActionToken switch
    {
        "AddList" => pActionPositiveBrush,
        "AddAll" => pActionPositiveBrush,
        "Execute" => pActionNegativeBrush,
        _ => null
    };
}
