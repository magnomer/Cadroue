using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PPanels;

public sealed class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    public event Action<string?>? PProcessingStepChange;

    private readonly StackPanel pProcessingRowPanel;
    private string? pProcessingStepCurrent;

    public PProcessing() : base("")
    {
        var pHeader = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = new TextBlock
            {
                Text = "Processing",
                FontSize = 12,
                FontFamily = pProcessingFontFamily,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        pProcessingRowPanel = new StackPanel();

        var pScroll = new ScrollViewer
        {
            Content = pProcessingRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pRoot);
    }

    public void PProcessingStepAdd(string pStepName, string pStepIconPath)
    {
        pProcessingRowPanel.Children.Add(PProcessingRowBuild(pStepName, pStepIconPath));
    }

    private Border PProcessingRowBuild(string pStepName, string pStepIconPath)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(pStepIconPath, pProcessingIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = pStepName,
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            VerticalAlignment = VerticalAlignment.Center
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = pStepName
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            pProcessingStepCurrent = pStepName;
            PProcessingSelectApply();
            PProcessingStepChange?.Invoke(pStepName);
            pRowEvent.Handled = true;
        };
        return pRowBorder;
    }

    private void PProcessingSelectApply()
    {
        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowName } pRowBorder)
            {
                pRowBorder.Background = pRowName == pProcessingStepCurrent
                    ? pProcessingSelectBrush
                    : Brushes.White;
            }
        }
    }

}
