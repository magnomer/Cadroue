using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private ToggleButton PInspectorToolBuild()
    {
        ToggleButton pToolButton = PCropToolBuild();
        PCropToolAttach(pToolButton);
        return pToolButton;
    }

    private ToggleButton PCropToolBuild()
    {
        pInspectorToolIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = QIcon.QIconRead(PCropIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };

        return new ToggleButton
        {
            Content = pInspectorToolIcon,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.DrawTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate()
        };
    }

    private void PCropToolAttach(ToggleButton pToolButton)
    {
        pToolButton.Checked += (_, _) => PCropToolHandle();
        pToolButton.Unchecked += (_, _) => QInspector.QInspectorToolSet(false);
    }

    private void PCropToolHandle()
    {
        QWhitebalance.QWhitebalanceToolSet(false, QWhitebalance.QWhitebalanceTarget);
        QInspector.QInspectorToolSet(true);
    }

    private static Style PInspectorToolCreate() => PInspectorToolCreate(typeof(ToggleButton));

    private static Style PInspectorToolCreate(Type pControlType)
    {
        var pTemplate = new ControlTemplate(pControlType);
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pStyle = new Style(pControlType);
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, pTemplate));
        return pStyle;
    }
}
