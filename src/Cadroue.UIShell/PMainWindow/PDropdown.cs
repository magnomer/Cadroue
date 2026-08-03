using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PMainWindow;

internal static class PDropdown
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PDropdownSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PDropdownTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PDropdownAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PDropdownHighlightBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xF3, 0xFF));

    internal static void PDropdownApply(ComboBox pCombo)
    {
        pCombo.IsEditable = false;
        pCombo.Template = PDropdownTemplateBuild(false);
        pCombo.ItemContainerStyle = PDropdownStyleBuild();
    }

    internal static void PDropdownEditableApply(ComboBox pCombo)
    {
        pCombo.IsEditable = true;
        pCombo.Template = PDropdownTemplateBuild(true);
        pCombo.ItemContainerStyle = PDropdownStyleBuild();
    }

    internal static void PDropdownEditableActionApply(
        ComboBox pCombo,
        string pActionTooltip)
    {
        pCombo.IsEditable = true;
        pCombo.Template = PDropdownTemplateBuild(true);
        pCombo.ItemContainerStyle = PDropdownActionStyleBuild(pActionTooltip);
    }

    private static ControlTemplate PDropdownTemplateBuild(bool pEditable)
    {
        var pTemplate = new ControlTemplate(typeof(ComboBox));
        var pRoot = new FrameworkElementFactory(typeof(Grid));
        var pBorder = PDropdownBorderBuild();
        pRoot.AppendChild(pBorder);

        var pDock = new FrameworkElementFactory(typeof(DockPanel));
        pBorder.AppendChild(pDock);
        pDock.AppendChild(PDropdownToggleBuild());
        pDock.AppendChild(pEditable ? PDropdownEditBuild() : PDropdownSelectBuild());
        pRoot.AppendChild(PDropdownPopupBuild());

        pTemplate.VisualTree = pRoot;
        PDropdownTriggerAdd(pTemplate);
        return pTemplate;
    }

    private static FrameworkElementFactory PDropdownBorderBuild()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "OuterBorder";
        pBorder.SetValue(Border.BackgroundProperty, Brushes.White);
        pBorder.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        return pBorder;
    }

    private static FrameworkElementFactory PDropdownToggleBuild()
    {
        var pToggle = new FrameworkElementFactory(typeof(ToggleButton));
        pToggle.SetValue(DockPanel.DockProperty, Dock.Right);
        pToggle.SetValue(FrameworkElement.WidthProperty, 26.0);
        pToggle.SetValue(ToggleButton.FocusableProperty, false);
        pToggle.SetValue(ToggleButton.BackgroundProperty, Brushes.Transparent);
        pToggle.SetValue(ToggleButton.BorderThicknessProperty, new Thickness(0));
        pToggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
        pToggle.SetValue(ToggleButton.TemplateProperty, PDropdownArrowBuild());
        pToggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });
        return pToggle;
    }

    private static FrameworkElementFactory PDropdownEditBuild()
    {
        var pEditableBox = new FrameworkElementFactory(typeof(TextBox));
        pEditableBox.Name = "PART_EditableTextBox";
        pEditableBox.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
        pEditableBox.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
        pEditableBox.SetValue(TextBox.ForegroundProperty, PDropdownTextBrush);
        pEditableBox.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        pEditableBox.SetValue(TextBox.PaddingProperty, new Thickness(14, 0, 10, 0));
        pEditableBox.SetValue(TextBox.SelectionBrushProperty, PDropdownAccentBrush);
        pEditableBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        pEditableBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        return pEditableBox;
    }

    private static FrameworkElementFactory PDropdownSelectBuild()
    {
        var pGrid = new FrameworkElementFactory(typeof(Grid));
        pGrid.AppendChild(PDropdownSurfaceBuild());
        pGrid.AppendChild(PDropdownContentBuild());
        return pGrid;
    }

    private static FrameworkElementFactory PDropdownSurfaceBuild()
    {
        var pToggle = new FrameworkElementFactory(typeof(ToggleButton));
        pToggle.SetValue(ToggleButton.FocusableProperty, false);
        pToggle.SetValue(ToggleButton.BackgroundProperty, Brushes.Transparent);
        pToggle.SetValue(ToggleButton.BorderThicknessProperty, new Thickness(0));
        pToggle.SetValue(ToggleButton.PaddingProperty, new Thickness(0));
        pToggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
        pToggle.SetValue(ToggleButton.TemplateProperty, PDropdownBlankBuild());
        pToggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });
        return pToggle;
    }

    private static ControlTemplate PDropdownBlankBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pTemplate.VisualTree = pBorder;
        return pTemplate;
    }

    private static FrameworkElementFactory PDropdownContentBuild()
    {
        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        pContent.SetValue(FrameworkElement.MarginProperty, new Thickness(14, 0, 10, 0));
        pContent.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pContent.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        pContent.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("SelectionBoxItemTemplate")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        pContent.SetBinding(ContentPresenter.ContentStringFormatProperty, new Binding("SelectionBoxItemStringFormat")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        return pContent;
    }

    private static FrameworkElementFactory PDropdownPopupBuild()
    {
        var pPopup = new FrameworkElementFactory(typeof(Popup));
        pPopup.Name = "PART_Popup";
        pPopup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        pPopup.SetValue(Popup.AllowsTransparencyProperty, true);
        pPopup.SetBinding(Popup.IsOpenProperty, new Binding("IsDropDownOpen")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });

        var pPopupBorder = new FrameworkElementFactory(typeof(Border));
        pPopupBorder.SetValue(Border.BackgroundProperty, Brushes.White);
        pPopupBorder.SetValue(Border.BorderBrushProperty, PLineBrush);
        pPopupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        pPopupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        pPopupBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 6, 0, 0));
        pPopupBorder.SetBinding(FrameworkElement.MinWidthProperty, new Binding("ActualWidth")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var pScroll = new FrameworkElementFactory(typeof(ScrollViewer));
        pScroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        pScroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        pScroll.SetValue(FrameworkElement.MaxHeightProperty, 260.0);

        var pItemsHost = new FrameworkElementFactory(typeof(StackPanel));
        pItemsHost.SetValue(Panel.IsItemsHostProperty, true);
        pScroll.AppendChild(pItemsHost);
        pPopupBorder.AppendChild(pScroll);
        pPopup.AppendChild(pPopupBorder);
        return pPopup;
    }

    private static void PDropdownTriggerAdd(ControlTemplate pTemplate)
    {
        var pFocusTrigger = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PDropdownAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pFocusTrigger);

        var pOpenTrigger = new Trigger { Property = ComboBox.IsDropDownOpenProperty, Value = true };
        pOpenTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PDropdownAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pOpenTrigger);
    }

    private static ControlTemplate PDropdownArrowBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1, 0, 0, 0));
        pBorder.SetValue(Border.BackgroundProperty, PDropdownSoftBrush);
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 10, 10, 0));

        var pArrow = new FrameworkElementFactory(typeof(Path));
        pArrow.SetValue(Path.StrokeProperty, PDropdownTextBrush);
        pArrow.SetValue(Path.StrokeThicknessProperty, 1.3);
        pArrow.SetValue(Path.StrokeStartLineCapProperty, PenLineCap.Round);
        pArrow.SetValue(Path.StrokeEndLineCapProperty, PenLineCap.Round);
        pArrow.SetValue(Path.StrokeLineJoinProperty, PenLineJoin.Round);
        pArrow.SetValue(Path.DataProperty, Geometry.Parse("M 3 4 L 6 7 L 9 4"));
        pArrow.SetValue(Path.WidthProperty, 9.0);
        pArrow.SetValue(Path.HeightProperty, 6.0);
        pArrow.SetValue(Path.StretchProperty, Stretch.Uniform);
        pArrow.SetValue(Path.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pArrow.SetValue(Path.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pArrow);

        pTemplate.VisualTree = pBorder;
        return pTemplate;
    }

    private static Style PDropdownStyleBuild()
    {
        return PDropdownStyleBuild(PDropdownRowBuild());
    }

    private static Style PDropdownStyleBuild(ControlTemplate pRowTemplate)
    {
        var pStyle = new Style(typeof(ComboBoxItem));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, pRowTemplate));
        return pStyle;
    }

    private static Style PDropdownActionStyleBuild(string pActionTooltip)
    {
        return PDropdownStyleBuild(PDropdownActionRowBuild(pActionTooltip));
    }

    private static ControlTemplate PDropdownRowBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ComboBoxItem));
        var pBorder = PDropdownRowBorderBuild(new Thickness(12, 6, 12, 6));

        var pDock = new FrameworkElementFactory(typeof(DockPanel));
        pBorder.AppendChild(pDock);
        pDock.AppendChild(PDropdownCheckBuild());

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pContent);

        pTemplate.VisualTree = pBorder;
        PDropdownRowAdd(pTemplate);
        return pTemplate;
    }

    private static ControlTemplate PDropdownActionRowBuild(string pActionTooltip)
    {
        var pTemplate = new ControlTemplate(typeof(ComboBoxItem));
        var pBorder = PDropdownRowBorderBuild(new Thickness(12, 6, 2, 6));

        var pDock = new FrameworkElementFactory(typeof(DockPanel));
        pBorder.AppendChild(pDock);

        var pAction = new FrameworkElementFactory(typeof(Button));
        pAction.SetValue(DockPanel.DockProperty, Dock.Right);
        pAction.SetValue(FrameworkElement.WidthProperty, 13.0);
        pAction.SetValue(FrameworkElement.HeightProperty, 13.0);
        pAction.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pAction.SetValue(Control.PaddingProperty, new Thickness(0));
        pAction.SetValue(FrameworkElement.StyleProperty, PButton.PButtonChromeCreate(false));
        pAction.SetValue(FrameworkElement.ToolTipProperty, pActionTooltip);
        pAction.SetValue(AutomationProperties.NameProperty, pActionTooltip);

        var pMinus = new FrameworkElementFactory(typeof(Image));
        pMinus.SetValue(Image.SourceProperty, PIcon.PIconRead("/PAssets/PPanels/PExportMinus.svg", PDropdownTextBrush));
        pMinus.SetValue(FrameworkElement.WidthProperty, 8.0);
        pMinus.SetValue(FrameworkElement.HeightProperty, 8.0);
        pMinus.SetValue(Image.StretchProperty, Stretch.Uniform);
        pAction.AppendChild(pMinus);
        pDock.AppendChild(pAction);

        FrameworkElementFactory pCheck = PDropdownCheckBuild();
        pCheck.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pCheck);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pContent);

        pTemplate.VisualTree = pBorder;
        PDropdownRowAdd(pTemplate);
        return pTemplate;
    }

    private static FrameworkElementFactory PDropdownRowBorderBuild(Thickness pPadding)
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "ItemBorder";
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        pBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(4));
        pBorder.SetValue(Border.PaddingProperty, pPadding);
        return pBorder;
    }

    private static FrameworkElementFactory PDropdownCheckBuild()
    {
        var pCheck = new FrameworkElementFactory(typeof(Path));
        pCheck.Name = "CheckPath";
        pCheck.SetValue(DockPanel.DockProperty, Dock.Right);
        pCheck.SetValue(Path.StrokeProperty, PDropdownAccentBrush);
        pCheck.SetValue(Path.StrokeThicknessProperty, 1.8);
        pCheck.SetValue(Path.StrokeStartLineCapProperty, PenLineCap.Round);
        pCheck.SetValue(Path.StrokeEndLineCapProperty, PenLineCap.Round);
        pCheck.SetValue(Path.DataProperty, Geometry.Parse("M 3 7 L 6 10 L 11 4"));
        pCheck.SetValue(Path.WidthProperty, 12.0);
        pCheck.SetValue(Path.HeightProperty, 12.0);
        pCheck.SetValue(Path.StretchProperty, Stretch.Fill);
        pCheck.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        pCheck.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        return pCheck;
    }

    private static void PDropdownRowAdd(ControlTemplate pTemplate)
    {
        var pHighlightTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHighlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, PDropdownHighlightBrush, "ItemBorder"));
        pTemplate.Triggers.Add(pHighlightTrigger);

        var pSelectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        pSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, PDropdownHighlightBrush, "ItemBorder"));
        pSelectedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "CheckPath"));
        pTemplate.Triggers.Add(pSelectedTrigger);
    }
}
