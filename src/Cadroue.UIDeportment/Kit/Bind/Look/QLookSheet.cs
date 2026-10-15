using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Cadroue.UIDeportment;

internal static class QLookSheet
{
    internal static readonly IReadOnlyList<QLook.QLookSetter> QLookSheetState =
    [
        new("PCheckbox", QLookCue.QLookCueHover, "PCheckboxBox", Border.BorderBrushProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueHover, "PCheckboxBox", Border.BackgroundProperty, "PCheckboxSoft"),
        new("PCheckbox", QLookCue.QLookCueChecked, "PCheckboxBox", Border.BackgroundProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueChecked, "PCheckboxBox", Border.BorderBrushProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueChecked, "PCheckboxMark", UIElement.OpacityProperty, "PCheckboxShown"),
        new("PCheckbox", QLookCue.QLookCueChecked | QLookCue.QLookCueHover, "PCheckboxBox",
            Border.BackgroundProperty, "PCheckboxDeep"),
        new("PCheckbox", QLookCue.QLookCueChecked | QLookCue.QLookCueHover, "PCheckboxBox",
            Border.BorderBrushProperty, "PCheckboxDeep"),
        new("PCheckbox", QLookCue.QLookCueMixed, "PCheckboxBox", Border.BackgroundProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueMixed, "PCheckboxBox", Border.BorderBrushProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueMixed, "PCheckboxDash", UIElement.VisibilityProperty, "PCheckboxVisible"),
        new("PCheckbox", QLookCue.QLookCueFocus, "PCheckboxBox", Border.BorderBrushProperty, "PCheckboxAccent"),
        new("PCheckbox", QLookCue.QLookCueDisabled, "PCheckboxFrame", UIElement.OpacityProperty, "PCheckboxFaded"),
        new("PCheckbox", QLookCue.QLookCueDisabled, "PCheckboxLabel",
            TextElement.ForegroundProperty, "PCheckboxMuted"),

        new("PSlider", QLookCue.QLookCueDisabled, "PSliderFrame", UIElement.OpacityProperty, "PSliderFaded"),

        new("PSliderThumb", QLookCue.QLookCueHover, "PSliderKnob", Shape.StrokeProperty, "PSliderDeep"),
        new("PSliderThumb", QLookCue.QLookCueHover, "PSliderKnob", Shape.FillProperty, "PSliderSoft"),
        new("PSliderThumb", QLookCue.QLookCueDrag, "PSliderKnob", Shape.StrokeProperty, "PSliderDeep"),
        new("PSliderThumb", QLookCue.QLookCueDrag, "PSliderKnob", Shape.StrokeThicknessProperty, "PSliderHeavy"),
    ];
}
