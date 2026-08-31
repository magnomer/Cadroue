using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private const double PSSheetTabWidth = 142;
    private const int PSSheetTabCount = 3;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetOutputIcon = "/PAssets/PTabs/PSSheetOutput.svg";
    private const string PSSheetVideoIcon = "/PAssets/PTabs/PSSheetVideo.svg";
    private const string PSSheetAudioIcon = "/PAssets/PTabs/PSSheetAudio.svg";

    private UIElement PSSheetControlBuild() => PSSheet.PSSheetControlBuild(
        PSSheetTabWidth,
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Output"), PSSheetOutputIcon, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSOutputBuild()))),
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Video"), PSSheetVideoIcon, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSVideoBuild()))),
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Audio"), PSSheetAudioIcon, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSAudioBuild()))));
}
