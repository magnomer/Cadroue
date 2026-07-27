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
    public event Action<LWorkPriority>? PActionRun;

    public PAction()
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Button pAddListButton = PActionButtonBuild("PActionAddList.svg", "Add List");
        Button pExecuteButton = PActionButtonBuild("PActionExecute.svg", "Execute");
        pAddListButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
        pExecuteButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pExecuteButton);
        Content = new Border { Child = pPanel };
    }

    private static Button PActionButtonBuild(string pIconAssetName, string pLabelText)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAssets/PCompass/{pIconAssetName}", PActionAccentBrushRead(pLabelText)),
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

    private static Brush? PActionAccentBrushRead(string pLabelText) => pLabelText switch
    {
        "Add List" => pActionPositiveBrush,
        "Execute" => pActionNegativeBrush,
        _ => null
    };
}
