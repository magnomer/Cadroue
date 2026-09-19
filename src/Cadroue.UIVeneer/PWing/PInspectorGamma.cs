using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
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

    private static readonly string[] PGammaLabelKeys =
    {
        "Inspector.Video.Midtone",
        "Inspector.Video.RedGamma",
        "Inspector.Video.GreenGamma",
        "Inspector.Video.BlueGamma",
        "Inspector.Video.HighlightProtection"
    };

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
        for (int pIndex = 0; pIndex < pGammaSliders.Length; pIndex++)
        {
            int pSlot = pIndex;
            double pLeast = pSlot == 4 ? 0 : -100;
            pGammaSliders[pSlot] = PToneSliderBuild(pLeast, 100, 0);
            pGammaValues[pSlot] = PInspectorDecimalBuild();
            pGammaValues[pSlot].Text = "0";
            PInspectorValueAttach(
                pGammaSliders[pSlot],
                pGammaValues[pSlot],
                pLeast,
                100,
                () => PGammaValueRead(pSlot),
                pNumber => LGamma.LGammaValueSet(pSlot, pNumber));
            pGammaStack.Children.Add(
                PFilterSliderBuild(
                    LLocalization.LLocalizationTextRead(PGammaLabelKeys[pSlot]),
                    pGammaSliders[pSlot],
                    pSlot == 4 ? "%" : "",
                    pGammaValues[pSlot]));
        }

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

    private double PGammaValueRead(int pSlot)
    {
        LWorkGammaSettings pGamma = LGamma.LGammaValue;
        return pSlot switch
        {
            1 => pGamma.LWorkGammaRed,
            2 => pGamma.LWorkGammaGreen,
            3 => pGamma.LWorkGammaBlue,
            4 => pGamma.LWorkGammaHighlight,
            _ => pGamma.LWorkGammaGlobal
        };
    }

    private void PGammaUpdate()
    {
        PInspectorSwitchUpdate(pGammaBox, LGamma.LGammaStep.LWorkStepActive, false);
        PInspectorSwitchUpdate(pGammaPersistent, LGamma.LGammaPersistent, true);
        for (int pSlot = 0; pSlot < pGammaSliders.Length; pSlot++)
        {
            PInspectorValueUpdate(pGammaSliders[pSlot], pGammaValues[pSlot], PGammaValueRead(pSlot), "0.#");
        }

        PInspectorSectionApply(
            pGammaBox, pGammaPersistent, pGammaStack, pGammaBody, LGamma.LGammaStep.LWorkStepActive,
            LGamma.LGammaCapable, LGamma.LGammaPreview,
            "Inspector.Video.GammaRequiresEq", "Inspector.Video.GammaPreviewMpv",
            "Inspector.Video.ApplyGamma", "Inspector.Video.PersistGamma");
    }
}
