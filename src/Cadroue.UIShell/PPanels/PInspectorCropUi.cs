using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private StackPanel PInspectorCropBodyBuild()
    {
        pInspectorInsetLeft = PInspectorInsetBuild();
        pInspectorInsetRight = PInspectorInsetBuild();
        pInspectorInsetTop = PInspectorInsetBuild();
        pInspectorInsetBottom = PInspectorInsetBuild();
        pInspectorRatioWidth = PInspectorRatioFieldBuild();
        pInspectorRatioHeight = PInspectorRatioFieldBuild();

        pInspectorRatioFixed = new CheckBox
        {
            Content = "Fixed ratio",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLabelWidth, 8, 0, 0)
        };
        pInspectorRatioFixed.Checked += (_, _) => PInspectorRatioCommit();
        pInspectorRatioFixed.Unchecked += (_, _) => PInspectorRatioCommit();

        pInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorFlipHorizontal = PInspectorFlipBuild("Horizontal");
        pInspectorFlipVertical = PInspectorFlipBuild("Vertical");
        pInspectorFlipHorizontal.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipHorizontal.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitchBuild(
            "Apply",
            "Apply the crop, rotation and flip to queued jobs");
        pInspectorApplyBox.Checked += (_, _) => PInspectorApplyUpdate();
        pInspectorApplyBox.Unchecked += (_, _) => PInspectorApplyUpdate();

        pInspectorCropStack = new StackPanel();
        pInspectorCropStack.Children.Add(PInspectorFieldBuild("Tool", pInspectorCropTool));
        pInspectorCropStack.Children.Add(PInspectorFieldBuild("Flip", PInspectorFlipRowBuild()));
        pInspectorCropStack.Children.Add(PInspectorFieldBuild("Rotate", pInspectorRotateCombo));
        pInspectorCropStack.Children.Add(PInspectorEdgeBuild());
        pInspectorCropStack.Children.Add(PInspectorRatioBuild());
        pInspectorCropStack.Children.Add(pInspectorRatioFixed);
        pInspectorCropStack.Children.Add(pInspectorRatioNotice);

        pInspectorCropBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorCropBody.Children.Add(pInspectorApplyBox);
        pInspectorCropBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorCropBody.Children.Add(pInspectorCropStack);
        PInspectorToolUpdate();
        PInspectorApplyUpdate();
        return pInspectorCropBody;
    }

    private UIElement PInspectorPersistentBuild()
    {
        pInspectorPersistentBox = PInspectorSwitchBuild(
            "Persistent",
            "Keep every crop setting untouched when a new media file is loaded");
        pInspectorPersistentBox.Margin = new Thickness(12, 0, 12, 12);
        pInspectorPersistentBox.Checked += (_, _) => PInspectorPersistentRaise();
        pInspectorPersistentBox.Unchecked += (_, _) => PInspectorPersistentRaise();

        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = PPanelLineBrush,
            Margin = new Thickness(12, 0, 12, 12)
        });
        pPersistentPanel.Children.Add(pInspectorPersistentBox);
        return pPersistentPanel;
    }

    private static CheckBox PInspectorSwitchBuild(string pSwitchLabel, string pSwitchTip) => new()
    {
        Content = pSwitchLabel,
        ToolTip = pSwitchTip,
        FontSize = 12,
        FontFamily = pInspectorFontFamily,
        Foreground = PPanelTextBrush,
        VerticalContentAlignment = VerticalAlignment.Center
    };

    private static UIElement PInspectorSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 12, 0, 12)
    };

    private ToggleButton PInspectorToolBuild()
    {
        pInspectorToolIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = PIcon.PIconRead(PInspectorCropIconPath, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };

        var pToolButton = new ToggleButton
        {
            Content = pInspectorToolIcon,
            ToolTip = "Draw the crop box on the preview",
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate()
        };
        pToolButton.Checked += (_, _) =>
        {
            PInspectorToolChange?.Invoke(true);
            PInspectorToolUpdate();
        };
        pToolButton.Unchecked += (_, _) =>
        {
            PInspectorToolChange?.Invoke(false);
            PInspectorCropClear();
            PInspectorToolUpdate();
        };
        return pToolButton;
    }

    private static Style PInspectorToolCreate()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pStyle = new Style(typeof(ToggleButton));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, pTemplate));
        return pStyle;
    }

    private ComboBox PInspectorRotateBuild()
    {
        var pRotateCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pRotateCombo);
        pRotateCombo.Items.Add("None");
        pRotateCombo.Items.Add("90° clockwise");
        pRotateCombo.Items.Add("180°");
        pRotateCombo.Items.Add("270° clockwise");
        pRotateCombo.SelectedIndex = 0;
        pRotateCombo.SelectionChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorRotateRaise();
        };
        return pRotateCombo;
    }

    private UIElement PInspectorEdgeBuild()
    {
        var pCropGrid = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        for (int pColumn = 0; pColumn < 3; pColumn++)
        {
            pCropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int pRow = 0; pRow < 3; pRow++)
        {
            pCropGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Top", pInspectorInsetTop), 0, 1);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Left", pInspectorInsetLeft), 1, 0);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Right", pInspectorInsetRight), 1, 2);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Bottom", pInspectorInsetBottom), 2, 1);
        return pCropGrid;
    }

    private static void PInspectorCellAdd(Grid pCropGrid, UIElement pCell, int pRow, int pColumn)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pCropGrid.Children.Add(pCell);
    }

    private static UIElement PInspectorCellBuild(string pCellLabel, TextBox pCellBox)
    {
        var pCellPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };
        pCellPanel.Children.Add(new TextBlock
        {
            Text = pCellLabel,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3)
        });
        pCellPanel.Children.Add(pCellBox);
        return pCellPanel;
    }

    private UIElement PInspectorRatioBuild()
    {
        var pRatioPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 0)
        };
        pRatioPanel.Children.Add(PInspectorLabelBuild("Ratio"));
        pRatioPanel.Children.Add(pInspectorRatioWidth);
        pRatioPanel.Children.Add(new TextBlock
        {
            Text = "×",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 7, 0)
        });
        pRatioPanel.Children.Add(pInspectorRatioHeight);
        return pRatioPanel;
    }

    private UIElement PInspectorFlipRowBuild()
    {
        var pFlipPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFlipPanel.Children.Add(pInspectorFlipHorizontal);
        pFlipPanel.Children.Add(pInspectorFlipVertical);
        return pFlipPanel;
    }

    private static CheckBox PInspectorFlipBuild(string pFlipLabel) => new()
    {
        Content = pFlipLabel,
        FontSize = 12,
        FontFamily = pInspectorFontFamily,
        Foreground = PPanelTextBrush,
        VerticalAlignment = VerticalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 14, 0)
    };

    private TextBox PInspectorRatioFieldBuild()
    {
        TextBox pRatioBox = PInspectorNumberBoxBuild();
        pRatioBox.TextChanged += (_, _) => PInspectorRatioEditHandle();
        return pRatioBox;
    }

    private TextBox PInspectorInsetBuild()
    {
        TextBox pInsetBox = PInspectorNumberBoxBuild();
        pInsetBox.TextChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorCropRaise();
        };
        return pInsetBox;
    }

    private static TextBox PInspectorNumberBoxBuild()
    {
        var pNumberBox = new TextBox
        {
            Text = "0",
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pNumberBox);
        pNumberBox.TextAlignment = TextAlignment.Center;
        pNumberBox.Padding = new Thickness(4, 0, 4, 0);
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !pNumberEvent.Text.All(char.IsDigit);
        return pNumberBox;
    }
}
