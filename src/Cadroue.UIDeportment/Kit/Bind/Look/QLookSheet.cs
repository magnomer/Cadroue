using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace Cadroue.UIDeportment;

internal static class QLookSheet
{
    internal static readonly IReadOnlyList<QLookSetter> QLookSheetState =
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

        new("PRadio", QLookCue.QLookCueHover, "PRadioSegmentBox", Border.BackgroundProperty, "PRadioSoft"),
        new("PRadio", QLookCue.QLookCueChecked, "PRadioSegmentBox", Border.BackgroundProperty, "PRadioAccent"),
        new("PRadio", QLookCue.QLookCueChecked, "PRadioSegmentLabel",
            TextElement.ForegroundProperty, "PRadioBright"),
        new("PRadio", QLookCue.QLookCueChecked | QLookCue.QLookCueHover, "PRadioSegmentBox",
            Border.BackgroundProperty, "PRadioDeep"),
        new("PRadio", QLookCue.QLookCueDisabled, "PRadioSegmentBox", UIElement.OpacityProperty, "PRadioFaded"),

        new("PScrollbarThumb", QLookCue.QLookCueHover, "PScrollbarKnob", Border.BackgroundProperty, "PScrollbarAccent"),
        new("PScrollbarThumb", QLookCue.QLookCueDrag, "PScrollbarKnob", Border.BackgroundProperty, "PScrollbarDeep"),

        new("PDropdown", QLookCue.QLookCueFocus, "PDropdownFrame", Border.BorderBrushProperty, "PDropdownAccent"),
        new("PDropdown", QLookCue.QLookCueExpanded, "PDropdownFrame", Border.BorderBrushProperty, "PDropdownAccent"),

        new("PDropdownRow", QLookCue.QLookCueHover, "PDropdownRowFrame",
            Border.BackgroundProperty, "PDropdownHighlight"),
        new("PDropdownRow", QLookCue.QLookCueSelected, "PDropdownRowFrame",
            Border.BackgroundProperty, "PDropdownHighlight"),
        new("PDropdownRow", QLookCue.QLookCueSelected, "PDropdownRowMark",
            UIElement.VisibilityProperty, "PDropdownVisible"),

        new("PDropdownRowButton", QLookCue.QLookCueHover, "PDropdownRowPlate",
            Border.BackgroundProperty, "PDropdownHover"),
        new("PDropdownRowButton", QLookCue.QLookCuePressed, "PDropdownRowPlate",
            Border.BackgroundProperty, "PDropdownPressed"),

        new("PTextbox", QLookCue.QLookCueFocus, "PTextboxFrame", Border.BorderBrushProperty, "PTextboxAccent"),

        new("PToken", QLookCue.QLookCueFocus, "PTokenFrame", Border.BorderBrushProperty, "PTokenAccent"),

        new("PTokenChip", QLookCue.QLookCueHover, "PTokenChipFrame", Border.BackgroundProperty, "PTokenHover"),
        new("PTokenChip", QLookCue.QLookCuePressed, "PTokenChipFrame", Border.BackgroundProperty, "PTokenPressed"),

        new("PPicker", QLookCue.QLookCueExpanded, "PPickerFrame", Border.BorderBrushProperty, "PPickerAccent"),

        new("PButtonSource", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonGreyHover"),
        new("PButtonSource", QLookCue.QLookCuePressed, "PButtonPlate",
            Border.BackgroundProperty, "PButtonGreyPressed"),

        new("PButtonCommand", QLookCue.QLookCueHover, "PButtonPlate",
            Border.BackgroundProperty, "PButtonCommandHover"),
        new("PButtonCommand", QLookCue.QLookCuePressed, "PButtonPlate",
            Border.BackgroundProperty, "PButtonCommandPressed"),

        new("PButtonFaded", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonCommandHover"),
        new("PButtonFaded", QLookCue.QLookCuePressed, "PButtonPlate",
            Border.BackgroundProperty, "PButtonCommandPressed"),
        new("PButtonFaded", QLookCue.QLookCueDisabled, "PButtonFrame", UIElement.OpacityProperty, "PButtonFade"),

        new("PButtonPanel", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonGreyHover"),
        new("PButtonPanel", QLookCue.QLookCueDisabled, "PButtonFace", TextElement.ForegroundProperty, "PButtonMuted"),

        new("PButtonWhite", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonWhiteHover"),
        new("PButtonWhite", QLookCue.QLookCuePressed, "PButtonPlate",
            Border.BackgroundProperty, "PButtonWhitePressed"),

        new("PButtonChrome", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonChromeHover"),
        new("PButtonChrome", QLookCue.QLookCuePressed, "PButtonPlate",
            Border.BackgroundProperty, "PButtonChromePressed"),

        new("PButtonExit", QLookCue.QLookCueHover, "PButtonPlate", Border.BackgroundProperty, "PButtonExitHover"),
        new("PButtonExit", QLookCue.QLookCueHover, "PButtonFace", TextElement.ForegroundProperty, "PButtonInk"),
        new("PButtonExit", QLookCue.QLookCuePressed, "PButtonPlate", Border.BackgroundProperty, "PButtonExitPressed"),
        new("PButtonExit", QLookCue.QLookCuePressed, "PButtonFace", TextElement.ForegroundProperty, "PButtonInk"),
    ];

    internal static IReadOnlyDictionary<(string, DependencyProperty), QLookSetter?> QLookSheetResolve(
        string lStyle, QLookCue lActive)
    {
        Dictionary<(string, DependencyProperty), QLookSetter?> lWinners = [];
        foreach (QLookSetter lRow in QLookSheetState.Where(lRow => lRow.QLookSetterStyle == lStyle))
        {
            (string, DependencyProperty) lSlot = (lRow.QLookSetterPart, lRow.QLookSetterProperty);
            if ((lActive & lRow.QLookSetterCue) == lRow.QLookSetterCue)
            {
                lWinners[lSlot] = lRow;
            }
            else
            {
                lWinners.TryAdd(lSlot, null);
            }
        }

        return lWinners;
    }
}
