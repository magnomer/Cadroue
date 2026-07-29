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

    public const double PProcessingStripWidth = 48;

    public event Action<string?>? PProcessingStepChange;
    public event Action<string>? PProcessingStepOpen;
    public event Action<bool>? PProcessingMinimizeChange;

    private readonly StackPanel pProcessingRowPanel;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;
    private string? pProcessingStepCurrent;
    private bool pProcessingMinimized;

    public PProcessing() : base("")
    {
        UIElement pHeader = PProcessingHeaderBuild();

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

        pProcessingFullBody = pRoot;
        pProcessingStripBody = PProcessingStripBuild();
        pProcessingStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pProcessingFullBody);
        pBodyHost.Children.Add(pProcessingStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
    }

    public bool PProcessingMinimizedCheck() => pProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest)
    {
        if (pProcessingMinimized == pProcessingMinimizeRequest)
        {
            return;
        }

        pProcessingMinimized = pProcessingMinimizeRequest;
        pProcessingFullBody.Visibility = pProcessingMinimized ? Visibility.Collapsed : Visibility.Visible;
        pProcessingStripBody.Visibility = pProcessingMinimized ? Visibility.Visible : Visibility.Collapsed;
        PProcessingMinimizeChange?.Invoke(pProcessingMinimized);
    }

    private UIElement PProcessingHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = "Processing",
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PProcessingButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", "Hide the Processing panel", () => PProcessingMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PProcessingStripBuild()
    {
        Button pMaximizeButton = PProcessingButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg", "Show the Processing panel", () => PProcessingMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pProcessingIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
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
            if (pRowEvent.ClickCount >= 2)
            {
                PProcessingStepOpen?.Invoke(pStepName);
            }

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
