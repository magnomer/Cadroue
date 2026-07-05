using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExportPanel
{
    private ComboBox PPresetComboBuild()
    {
        var pCombo = new ComboBox
        {
            IsEditable = true,
            StaysOpenOnEdit = true,
            ItemsSource = LExportSpecificState.LPresetNames,
            Text = lExportSpecificState.PresetName,
            Height = 40,
            Background = Brushes.White,
            BorderBrush = PLineBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Padding = new Thickness(0)
        };
        PMainDropdown.PMainDropdownEditableApply(pCombo);
        pCombo.SelectionChanged += (_, _) => PPresetSelectApply();
        return pCombo;
    }

    private UIElement PPresetTopRowBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.Children.Add(pPresetCombo);

        var pSaveButton = PIconButtonBuild(PSaveIconBuild(), PPresetSave);
        PTopIconButtonColumnSet(pSaveButton, 1);
        pGrid.Children.Add(pSaveButton);

        var pDeleteButton = PIconButtonBuild(PDeleteIconBuild(), PPresetDelete);
        PTopIconButtonColumnSet(pDeleteButton, 2);
        pGrid.Children.Add(pDeleteButton);
        return pGrid;
    }

    private static void PTopIconButtonColumnSet(Button pButton, int pColumn)
    {
        Grid.SetColumn(pButton, pColumn);
    }

    private UIElement PPresetActionRowBuild()
    {
        var pPanel = new UniformGrid { Columns = 3 };
        pPanel.Children.Add(PNormalIconButtonBuild("Settings", PSettingsIconBuild(), PSpecificOpen));
        pPanel.Children.Add(PNormalIconButtonBuild("Import", PImportIconBuild(), null));
        pPanel.Children.Add(PNormalIconButtonBuild("Export", PExportIconBuild(), null));
        return pPanel;
    }

    private static StackPanel PActionButtonContentBuild(string pText, UIElement pIcon)
    {
        var pStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pStack.Children.Add(pIcon);

        if (!string.IsNullOrWhiteSpace(pText))
        {
            pStack.Children.Add(new TextBlock
            {
                Text = pText,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = PTextBrush,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return pStack;
    }

    private Button PNormalIconButtonBuild(string pText, UIElement pIcon, RoutedEventHandler? pClick)
    {
        var pButton = new Button
        {
            Content = PActionButtonContentBuild(pText, pIcon),
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Style = PMainButton.PButtonNormalIconWhiteCreate()
        };

        if (pClick is not null)
        {
            pButton.Click += pClick;
        }

        return pButton;
    }

    private Button PIconButtonBuild(UIElement pIcon, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = pIcon,
            Margin = new Thickness(10, 0, 0, 0),
            Style = PMainButton.PButtonIconWhiteCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private void PPresetSelectApply()
    {
        if (pPresetCombo.SelectedItem is not string lPresetName)
        {
            return;
        }

        if (LExportSpecificState.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryRefresh();
        }
    }

    private void PPresetSave(object sender, RoutedEventArgs e)
    {
        string lPresetName = pPresetCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return;
        }

        lExportSpecificState.PresetName = lPresetName;
        LExportSpecificState.LPresetSave(lPresetName, lExportSpecificState);
        pPresetCombo.SelectedItem = lPresetName;
        PExportSummaryRefresh();
    }

    private void PPresetDelete(object sender, RoutedEventArgs e)
    {
        string lPresetName = pPresetCombo.Text.Trim();
        if (!LExportSpecificState.LPresetDelete(lPresetName))
        {
            return;
        }

        string? lNextPresetName = LExportSpecificState.LPresetFirstName;
        if (lNextPresetName is not null && LExportSpecificState.LPresetTryLoad(lNextPresetName, lExportSpecificState))
        {
            pPresetCombo.SelectedItem = lNextPresetName;
        }
        else
        {
            lExportSpecificState.PresetName = string.Empty;
            pPresetCombo.Text = string.Empty;
        }

        PExportSummaryRefresh();
    }

    private void PSpecificOpen(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        var psExportSpecific = new PSExportSpecific(lExportSpecificState, PExportSummaryRefresh)
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psExportSpecific.ShowDialog() == true)
        {
            PExportSummaryRefresh();
        }
    }

    private static UIElement PSaveIconBuild()
    {
        var pCanvas = new Canvas { Width = 20, Height = 20, VerticalAlignment = VerticalAlignment.Center };
        pCanvas.Children.Add(PRectBuild(4, 3, 12, 14));
        pCanvas.Children.Add(PLineBuild(6, 6, 14, 6));
        pCanvas.Children.Add(PLineBuild(8, 11, 12, 11));
        pCanvas.Children.Add(PLineBuild(8, 13, 12, 13));
        return pCanvas;
    }

    private static UIElement PDeleteIconBuild()
    {
        var pCanvas = new Canvas { Width = 20, Height = 20, VerticalAlignment = VerticalAlignment.Center };
        Brush pBrush = new SolidColorBrush(Color.FromRgb(0xC9, 0x2A, 0x2A));
        foreach ((double x1, double y1, double x2, double y2) in new[] { (6d, 6d, 14d, 6d), (8d, 4d, 12d, 4d), (7d, 8d, 7d, 16d), (13d, 8d, 13d, 16d), (8.5d, 10d, 8.5d, 14d), (11.5d, 10d, 11.5d, 14d), (7d, 16d, 13d, 16d) })
        {
            pCanvas.Children.Add(PLineBuild(x1, y1, x2, y2, pBrush));
        }
        return pCanvas;
    }

    private static UIElement PSettingsIconBuild()
    {
        var pCanvas = PIconCanvasBuild();
        pCanvas.Children.Add(PLineBuild(3, 5, 17, 5));
        pCanvas.Children.Add(PLineBuild(3, 10, 17, 10));
        pCanvas.Children.Add(PLineBuild(3, 15, 17, 15));
        pCanvas.Children.Add(PCircleBuild(7, 5));
        pCanvas.Children.Add(PCircleBuild(13, 10));
        pCanvas.Children.Add(PCircleBuild(9, 15));
        return pCanvas;
    }

    private static UIElement PImportIconBuild()
    {
        var pCanvas = PIconCanvasBuild();
        foreach ((double x1, double y1, double x2, double y2) in new[] { (10d, 3d, 10d, 12d), (6.5d, 8.5d, 10d, 12d), (13.5d, 8.5d, 10d, 12d), (5d, 16d, 15d, 16d), (5d, 16d, 5d, 13d), (15d, 16d, 15d, 13d) })
        {
            pCanvas.Children.Add(PLineBuild(x1, y1, x2, y2));
        }
        return pCanvas;
    }

    private static UIElement PExportIconBuild()
    {
        var pCanvas = PIconCanvasBuild();
        foreach ((double x1, double y1, double x2, double y2) in new[] { (10d, 14d, 10d, 5d), (6.5d, 8.5d, 10d, 5d), (13.5d, 8.5d, 10d, 5d), (5d, 16d, 15d, 16d), (5d, 16d, 5d, 13d), (15d, 16d, 15d, 13d) })
        {
            pCanvas.Children.Add(PLineBuild(x1, y1, x2, y2));
        }
        return pCanvas;
    }

    private static Canvas PIconCanvasBuild() => new()
    {
        Width = 20,
        Height = 20,
        Margin = new Thickness(0, 0, 8, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Rectangle PRectBuild(double pLeft, double pTop, double pWidth, double pHeight)
    {
        var pRect = new Rectangle
        {
            Width = pWidth,
            Height = pHeight,
            Stroke = PTextBrush,
            Fill = Brushes.Transparent,
            StrokeThickness = 1.7,
            RadiusX = 1,
            RadiusY = 1
        };
        Canvas.SetLeft(pRect, pLeft);
        Canvas.SetTop(pRect, pTop);
        return pRect;
    }

    private static Line PLineBuild(double pX1, double pY1, double pX2, double pY2)
    {
        return PLineBuild(pX1, pY1, pX2, pY2, PTextBrush);
    }

    private static Line PLineBuild(double pX1, double pY1, double pX2, double pY2, Brush pBrush) => new()
    {
        X1 = pX1,
        Y1 = pY1,
        X2 = pX2,
        Y2 = pY2,
        Stroke = pBrush,
        StrokeThickness = 1.8,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round
    };

    private static Ellipse PCircleBuild(double pCenterX, double pCenterY)
    {
        var pCircle = new Ellipse
        {
            Width = 5,
            Height = 5,
            Stroke = PTextBrush,
            Fill = Brushes.White,
            StrokeThickness = 1.8
        };
        Canvas.SetLeft(pCircle, pCenterX - 2.5);
        Canvas.SetTop(pCircle, pCenterY - 2.5);
        return pCircle;
    }
}
