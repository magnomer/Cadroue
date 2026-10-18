using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterEntry
{
    private static readonly IReadOnlyDictionary<string, Func<QRoster, QRosterEntry, ContextMenu, UIElement>>
        PRosterEntryBuilders = new Dictionary<string, Func<QRoster, QRosterEntry, ContextMenu, UIElement>>
        {
            [QRosterEntry.QRosterKindHead] = (lRoster, lEntry, _) =>
                PRosterCard.PRosterHeaderBuild(lRoster, lEntry.QRosterEntryCard),
            [QRosterEntry.QRosterKindFile] = (_, lEntry, _) => PRosterFileBuild(lEntry.QRosterEntryFile!),
            [QRosterEntry.QRosterKindRow] = (lRoster, lEntry, pMenu) =>
                PRosterRow.PRosterRowBuild(lRoster, lEntry.QRosterEntryRow!, pMenu)
        };

    public static Border PRosterEntryBuild(QRoster lRoster, QRosterEntry lEntry, ContextMenu pMenu) => new()
    {
        Background = PRosterTheme.PRosterBodyFills[lEntry.QRosterEntryCard.QRosterCardBody],
        BorderBrush = PRosterTheme.PRosterBodyLines[lEntry.QRosterEntryCard.QRosterCardBody],
        BorderThickness = PRosterTheme.PRosterFrameBorders[lEntry.QRosterEntryFrame],
        CornerRadius = PRosterTheme.PRosterFrameCorners[lEntry.QRosterEntryFrame],
        Padding = PRosterTheme.PRosterFramePaddings[lEntry.QRosterEntryFrame],
        Margin = PRosterTheme.PRosterFrameMargins[lEntry.QRosterEntryFrame],
        Child = PRosterEntryBuilders[lEntry.QRosterEntryKind](lRoster, lEntry, pMenu)
    };

    private static Border PRosterFileBuild(QRosterFile lFile) => new()
    {
        Background = PRosterTheme.PRosterRowShades[lFile.QRosterFileShade],
        Padding = new Thickness(12, 5, 12, 3),
        Child = new TextBlock
        {
            Text = lFile.QRosterFileTitle,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        }
    };
}
