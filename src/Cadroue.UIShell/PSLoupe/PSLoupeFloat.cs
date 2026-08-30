using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell;

internal enum PSLoupeFloat
{
    PSLoupeFloatOff,
    PSLoupeFloatOwner,
    PSLoupeFloatTop
}

internal sealed partial class PSLoupe
{
    private UIElement PSLoupeFloatBuild()
    {
        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        psLoupeFloatButtons = new[]
        {
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatOff, "Loupe.Float.Off"),
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatOwner, "Loupe.Float.Owner"),
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatTop, "Loupe.Float.Top")
        };

        foreach (Button pSegment in psLoupeFloatButtons)
        {
            pStrip.Children.Add(pSegment);
        }

        PSLoupeFloatShow();
        return new Border
        {
            BorderBrush = PSLoupeFloatBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Child = pStrip
        };
    }

    private Button PSLoupeSegmentBuild(PSLoupeFloat pMode, string pLabelKey)
    {
        var pButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead(pLabelKey),
            Height = 24,
            MinWidth = 52,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Tag = pMode,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PSLoupeFloatSelect(pMode);
        return pButton;
    }

    private void PSLoupeFloatSelect(PSLoupeFloat pMode)
    {
        psLoupeFloat = pMode;
        PSLoupeFloatShow();
        PSLoupeFloatApply(pMode);
        LPreferenceState pPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pPreference.LPreferenceLoupeFloat = PSLoupeTokenRead(pMode);
        LPreference.LPreferenceStateSet(pPreference);
    }

    private void PSLoupeFloatShow()
    {
        if (psLoupeFloatButtons is null)
        {
            return;
        }

        foreach (Button pSegment in psLoupeFloatButtons)
        {
            bool pActive = pSegment.Tag is PSLoupeFloat pMode && pMode == psLoupeFloat;
            pSegment.Background = pActive ? PSLoupeFloatFill : Brushes.Transparent;
        }
    }

    private void PSLoupeFloatApply(PSLoupeFloat pMode)
    {
        switch (pMode)
        {
            case PSLoupeFloat.PSLoupeFloatOff:
                Owner = null;
                Topmost = false;
                break;
            case PSLoupeFloat.PSLoupeFloatOwner:
                Owner = psLoupeOwner;
                Topmost = false;
                break;
            case PSLoupeFloat.PSLoupeFloatTop:
                Owner = psLoupeOwner;
                Topmost = true;
                break;
        }
    }

    private static PSLoupeFloat PSLoupeFloatRestore()
    {
        return LPreference.LPreferenceStateCurrent.LPreferenceLoupeFloat switch
        {
            "Off" => PSLoupeFloat.PSLoupeFloatOff,
            "Top" => PSLoupeFloat.PSLoupeFloatTop,
            _ => PSLoupeFloat.PSLoupeFloatOwner
        };
    }

    private static string PSLoupeTokenRead(PSLoupeFloat pMode)
    {
        return pMode switch
        {
            PSLoupeFloat.PSLoupeFloatOff => "Off",
            PSLoupeFloat.PSLoupeFloatTop => "Top",
            _ => "Owner"
        };
    }
}
