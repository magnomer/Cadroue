using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PSection
{
    private TextBlock PSectionTextBuild(int pSectionIndex, LPiece pSectionEntry)
    {
        bool pSectionUnnamed = string.IsNullOrEmpty(pSectionEntry.LPieceName);
        var pMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
        var pNameText = new TextBlock
        {
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        pNameText.Inlines.Add(new System.Windows.Documents.Run(
            pSectionUnnamed ? PSectionPlaceholderFormat(pSectionIndex) : pSectionEntry.LPieceName)
        {
            Foreground = pSectionUnnamed ? pMutedBrush : pNameText.Foreground
        });

        PSectionAffixAdd(pNameText, pSectionEntry.LPiecePrefix, pMutedBrush);
        PSectionAffixAdd(pNameText, pSectionEntry.LPieceSuffix, pMutedBrush);
        return pNameText;
    }

    private static void PSectionAffixAdd(TextBlock pNameText, string pAffixValue, Brush pMutedBrush)
    {
        if (string.IsNullOrEmpty(pAffixValue))
        {
            return;
        }

        pNameText.Inlines.Add(new System.Windows.Documents.Run($"  /  {pAffixValue}") { Foreground = pMutedBrush });
    }

    private static string PSectionPlaceholderFormat(int pSectionIndex)
        => LLocalization.LLocalizationFormat("Section.DefaultName", pSectionIndex + 1);
}
