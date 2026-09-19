using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIVeneer;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PCompass
{
    private static readonly Brush pCompassAccentBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED));
    private static readonly Brush pCompassRestBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA3));
    private const string PCompassWaveformIcon = "/PAsset/PCompass/PCompassWaveform.svg";
    private Image pCompassWaveformIcon = null!;

    private Image pCompassPlayImage = null!;
    private TextBlock pCompassPlayLabel = null!;
    private Button pCompassPlayButton = null!;

    private Button PCompassWaveformBuild(PFlow pFlow)
    {
        pCompassWaveformIcon = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pCompassWaveformIcon);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Compass.Waveform.Label"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });

        var pButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Waveform.Tooltip")
        };
        pButton.Click += (_, _) => pFlow.PFlowWaveformSet(!pFlow.LFlow.LFlowWaveformActive);
        return pButton;
    }

    private void PCompassWaveformApply(bool pCompassWaveformActive)
    {
        pCompassWaveformIcon.Source = PIcon.PIconRead(
            PCompassWaveformIcon,
            pCompassWaveformActive ? pCompassAccentBrush : pCompassRestBrush);
    }

    private static Button PCompassButtonBuild(
        PCompassAction pAction,
        string pIconAssetName,
        string pLabelKey,
        string pTooltipKey)
    {
        string pLabelText = LLocalization.LLocalizationTextRead(pLabelKey);
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAsset/PCompass/{pIconAssetName}", PCompassAccentRead(pAction)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });
        return new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate(),
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey)
        };
    }

    private Button PCompassToggleBuild()
    {
        pCompassPlayImage = new Image
        {
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        pCompassPlayLabel = new TextBlock
        {
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(pCompassPlayImage);
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(pCompassPlayLabel);
        pCompassPlayButton = new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Style = PHouse.PButton.PButtonCommandCreate()
        };
        PCompassPlayingApply(false);
        return pCompassPlayButton;
    }

    private void PCompassPlayingApply(bool pCompassPlayingNow)
    {
        PCompassAction pAction = pCompassPlayingNow ? PCompassAction.PCompassStandby : PCompassAction.PCompassPlayback;
        string pIcon = pCompassPlayingNow ? "PCompassPause.svg" : "PCompassPlay.svg";
        string pLabelKey = pCompassPlayingNow ? "Compass.Pause.Label" : "Compass.Play.Label";
        string pTooltipKey = pCompassPlayingNow ? "Compass.Pause.Tooltip" : "Compass.Play.Tooltip";
        pCompassPlayImage.Source = PIcon.PIconRead($"/PAsset/PCompass/{pIcon}", PCompassAccentRead(pAction));
        pCompassPlayLabel.Text = LLocalization.LLocalizationTextRead(pLabelKey);
        pCompassPlayButton.ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey);
    }

    private void PCompassPlayToggle()
    {
        if (LCompass.LCompassPlaying)
        {
            pCompassFlow.PFlowPauseRaise();
        }
        else
        {
            pCompassFlow.PFlowPlayRaise();
        }
    }
}
