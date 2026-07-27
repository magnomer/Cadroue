using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    public event Action<string?>? PProcessingStepChange;

    private readonly TextBlock pProcessingCountLabel;
    private readonly StackPanel pProcessingRowPanel;
    private string? pProcessingStepCurrent;

    public PProcessing() : base("")
    {
        pProcessingCountLabel = new TextBlock
        {
            Text = "Processing",
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var pHeader = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pProcessingCountLabel
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
        UIElement pActionBar = PProcessingActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pRoot);
    }

    public void PProcessingStepAdd(string pStepName, string pStepIconPath)
    {
        pProcessingRowPanel.Children.Add(PProcessingRowBuild(pStepName, pStepIconPath));
        int pStepCount = pProcessingRowPanel.Children.Count;
        pProcessingCountLabel.Text = $"Processing  ({pStepCount})";
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

    private UIElement PProcessingActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PProcessingButtonBuild(
            "/PAssets/PPanels/PExportPlus.svg",
            "Add a processing step"));
        pActionLeft.Children.Add(PProcessingButtonBuild(
            "/PAssets/PPanels/PExportMinus.svg",
            "Delete the selected processing step"));

        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Button pProcessingSortButton = PProcessingButtonBuild(
            "/PAssets/PPanels/PSort.svg",
            "Sort the processing steps");
        pProcessingSortButton.Margin = new Thickness(0);
        pActionRight.Children.Add(pProcessingSortButton);

        var pActionPanel = new Grid { Margin = new Thickness(10, 4, 10, 4) };
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pActionRight, 1);
        pActionPanel.Children.Add(pActionLeft);
        pActionPanel.Children.Add(pActionRight);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionPanel
        };
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip) => new()
    {
        Content = new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(pIconPath, new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
            Stretch = Stretch.Uniform
        },
        ToolTip = pTooltip,
        Width = 28,
        Height = 26,
        Margin = new Thickness(0, 0, 2, 0),
        Style = PButton.PButtonPanelCreate(),
        IsEnabled = false
    };
}
