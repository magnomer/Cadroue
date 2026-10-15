using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private const double PSSheetTabWidth = 142;
    private const int PSSheetTabCount = 3;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetOutputIcon = "/PAsset/PTab/PSSheetOutput.svg";
    private const string PSSheetVideoIcon = "/PAsset/PTab/PSSheetVideo.svg";
    private const string PSSheetAudioIcon = "/PAsset/PTab/PSSheetAudio.svg";

    private UIElement PSSheetControlBuild() => PSSheet.PSSheetControlBuild(
        PSSheetTabWidth,
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Output"),
            PSSheetOutputIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSOutputBuild()))),
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Video"),
            PSSheetVideoIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSVideoBuild()))),
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Audio"),
            PSSheetAudioIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSAudioBuild()))));
}
