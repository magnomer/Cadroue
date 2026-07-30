using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private const double PSSheetTabWidth = 142;
    private const int PSSheetTabCount = 3;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetOutputIconPath = "/PAssets/PTabs/PSSheetOutput.svg";
    private const string PSSheetVideoIconPath = "/PAssets/PTabs/PSSheetVideo.svg";
    private const string PSSheetAudioIconPath = "/PAssets/PTabs/PSSheetAudio.svg";

    private UIElement PSCasementOverlayBuild() =>
        PSCasement.PSCasementOverlayBuild(this, PSSheetStripWidth);

    private UIElement PSSheetControlBuild() => PSSheet.PSSheetControlBuild(
        PSSheetTabWidth,
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Output"), PSSheetOutputIconPath, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSOutputBuild()))),
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Video"), PSSheetVideoIconPath, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSVideoBuild()))),
        PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Encoder.Sheet.Audio"), PSSheetAudioIconPath, PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSAudioBuild()))));
}
