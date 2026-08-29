using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pGammaBox = null!;
    private CheckBox pGammaPersistent = null!;
    private Slider pGammaSlider = null!;
    private TextBox pGammaValue = null!;
    private Slider pGammaRedSlider = null!;
    private TextBox pGammaRedValue = null!;
    private Slider pGammaGreenSlider = null!;
    private TextBox pGammaGreenValue = null!;
    private Slider pGammaBlueSlider = null!;
    private TextBox pGammaBlueValue = null!;
    private Slider pGammaHighlightSlider = null!;
    private TextBox pGammaHighlightValue = null!;
    private StackPanel pGammaStack = null!;
    private StackPanel pGammaBody = null!;
    private bool pGammaCapable;
    private bool pGammaPreview;
    private string pGammaDisabledKey = "Inspector.Video.GammaRequiresEq";

    private StackPanel PGammaBuild()
    {
        pGammaBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyGamma"));
        pGammaPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistGamma"));
        pGammaSlider = PToneSliderBuild(-100, 100, 0);
        pGammaValue = PInspectorDecimalBuild();
        pGammaValue.Text = "0";
        pGammaRedSlider = PToneSliderBuild(-100, 100, 0);
        pGammaRedValue = PInspectorDecimalBuild();
        pGammaRedValue.Text = "0";
        pGammaGreenSlider = PToneSliderBuild(-100, 100, 0);
        pGammaGreenValue = PInspectorDecimalBuild();
        pGammaGreenValue.Text = "0";
        pGammaBlueSlider = PToneSliderBuild(-100, 100, 0);
        pGammaBlueValue = PInspectorDecimalBuild();
        pGammaBlueValue.Text = "0";
        pGammaHighlightSlider = PToneSliderBuild(0, 100, 0);
        pGammaHighlightValue = PInspectorDecimalBuild();
        pGammaHighlightValue.Text = "0";
        pGammaStack = new StackPanel();
        PInspectorVideoAttach(
            pGammaBox,
            pGammaStack,
            pGammaSlider,
            pGammaValue,
            -100,
            100,
            "0.#");
        PInspectorValueAttach(
            pGammaRedSlider, pGammaRedValue, -100, 100, "0.#");
        PInspectorValueAttach(
            pGammaGreenSlider, pGammaGreenValue, -100, 100, "0.#");
        PInspectorValueAttach(
            pGammaBlueSlider, pGammaBlueValue, -100, 100, "0.#");
        PInspectorValueAttach(
            pGammaHighlightSlider, pGammaHighlightValue, 0, 100, "0.#");
        pGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.Midtone"), pGammaSlider, "", pGammaValue));
        pGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.RedGamma"), pGammaRedSlider, "", pGammaRedValue));
        pGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.GreenGamma"), pGammaGreenSlider, "", pGammaGreenValue));
        pGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.BlueGamma"), pGammaBlueSlider, "", pGammaBlueValue));
        pGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.HighlightProtection"), pGammaHighlightSlider, "%", pGammaHighlightValue));
        var pGammaReset = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Video.GammaReset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.GammaResetTooltip"),
            Height = 28,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pGammaReset.Click += (_, _) => PGammaReset();
        pGammaStack.Children.Add(pGammaReset);
        pGammaBody = PToneBodyBuild(pGammaBox, pGammaStack);
        PToneApplyUpdate(pGammaBox, pGammaStack);
        return pGammaBody;
    }

    public void PGammaCapabilitySet(bool pGammaCapable, bool pGammaPreview, string pGammaDisabledKey)
    {
        this.pGammaCapable = pGammaCapable;
        this.pGammaPreview = pGammaPreview;
        this.pGammaDisabledKey = pGammaDisabledKey;
        PInspectorSectionApply(
            pGammaBox, pGammaPersistent, pGammaStack, pGammaBody,
            pGammaCapable, pGammaPreview, pGammaDisabledKey, "Inspector.Video.GammaPreviewMpv",
            "Inspector.Video.ApplyGamma", "Inspector.Video.PersistGamma");
    }

    private void PGammaReset()
    {
        bool pPrevious = pInspectorVideoSuppress;
        bool pChanged = PInspectorDecimalRead(pGammaValue, pGammaSlider.Value) != 0
            || PInspectorDecimalRead(pGammaRedValue, pGammaRedSlider.Value) != 0
            || PInspectorDecimalRead(pGammaGreenValue, pGammaGreenSlider.Value) != 0
            || PInspectorDecimalRead(pGammaBlueValue, pGammaBlueSlider.Value) != 0
            || PInspectorDecimalRead(pGammaHighlightValue, pGammaHighlightSlider.Value) != 0;
        pInspectorVideoSuppress = true;
        try
        {
            PInspectorValueSet(pGammaSlider, pGammaValue, 0);
            PInspectorValueSet(pGammaRedSlider, pGammaRedValue, 0);
            PInspectorValueSet(pGammaGreenSlider, pGammaGreenValue, 0);
            PInspectorValueSet(pGammaBlueSlider, pGammaBlueValue, 0);
            PInspectorValueSet(pGammaHighlightSlider, pGammaHighlightValue, 0);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        if (!pPrevious && pChanged)
        {
            PInspectorVideoChange?.Invoke();
        }
    }
}
