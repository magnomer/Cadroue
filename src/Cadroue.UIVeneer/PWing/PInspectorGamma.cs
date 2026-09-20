using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pGammaBox = null!;
    private CheckBox pGammaPersistent = null!;
    private StackPanel pGammaStack = null!;
    private StackPanel pGammaBody = null!;
    private readonly Slider[] pGammaSliders = new Slider[5];
    private readonly TextBox[] pGammaValues = new TextBox[5];
    private readonly List<int> pGammaSlots = [0, 1, 2, 3, 4];

    private static readonly string[] PGammaLabelKeys =
    {
        "Inspector.Video.Midtone",
        "Inspector.Video.RedGamma",
        "Inspector.Video.GreenGamma",
        "Inspector.Video.BlueGamma",
        "Inspector.Video.HighlightProtection"
    };

    private static readonly string[] PGammaUnits = { "", "", "", "", "%" };

    private StackPanel PGammaBuild()
    {
        pGammaBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyGamma"));
        pGammaPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistGamma"));
        PInspectorSwitchAttach(pGammaBox, LGamma.LGammaActiveSet);
        PInspectorSwitchAttach(pGammaPersistent, LGamma.LGammaPersistentSet);
        pGammaStack = new StackPanel();
        pGammaSlots.ForEach(PGammaRowBuild);

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
        pGammaReset.Click += (_, _) => LGamma.LGammaReset();
        pGammaStack.Children.Add(pGammaReset);
        pGammaBody = PToneBodyBuild(pGammaBox, pGammaStack);
        return pGammaBody;
    }

    private void PGammaRowBuild(int pSlot)
    {
        pGammaSliders[pSlot] = PToneSliderBuild(LGamma.LGammaLeastRead(pSlot), 100, 0);
        pGammaValues[pSlot] = PInspectorDecimalBuild();
        pGammaValues[pSlot].Text = "0";
        PInspectorValueAttach(
            pGammaSliders[pSlot],
            pGammaValues[pSlot],
            LGamma.LGammaLeastRead(pSlot),
            100,
            () => LGamma.LGammaValueRead(pSlot),
            pNumber => LGamma.LGammaValueSet(pSlot, pNumber));
        pGammaStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead(PGammaLabelKeys[pSlot]),
                pGammaSliders[pSlot],
                PGammaUnits[pSlot],
                pGammaValues[pSlot]));
    }

    private void PGammaSlotUpdate(int pSlot) =>
        PInspectorValueUpdate(pGammaSliders[pSlot], pGammaValues[pSlot], LGamma.LGammaValueRead(pSlot), "0.#");

    private void PGammaUpdate()
    {
        PInspectorSwitchUpdate(pGammaBox, LGamma.LGammaStep.LWorkStepActive);
        PInspectorSwitchUpdate(pGammaPersistent, LGamma.LGammaPersistent);
        pGammaSlots.ForEach(PGammaSlotUpdate);
        PInspectorSectionApply(pGammaBox, pGammaPersistent, pGammaStack, pGammaBody, LInspector.LInspectorTipResolve(
            LGamma.LGammaStep.LWorkStepActive,
            LGamma.LGammaCapable,
            LGamma.LGammaPreview,
            "Inspector.Video.GammaRequiresEq",
            "Inspector.Video.GammaPreviewMpv",
            "Inspector.Video.ApplyGamma",
            "Inspector.Video.PersistGamma"));
    }
}
