using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PListStack
{
    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pListLockedBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));

    private static readonly IReadOnlyDictionary<bool, Func<PListStack, QListCard, UIElement>> pListCards =
        new Dictionary<bool, Func<PListStack, QListCard, UIElement>>
        {
            [false] = (pStack, lCard) => pStack.PListRowBuild(lCard.QListCardRows[0]),
            [true] = (pStack, lCard) => pStack.PListLockedBuild(lCard),
        };

    private readonly StackPanel pListRowPanel = new();
    private readonly TextBlock pListEmptyNotice;
    private readonly List<PListRow> pListRows = [];
    private readonly UIElement pListFocus;
    private readonly QList lList;

    public PListStack(UIElement pFocus, QList lListOwner)
    {
        pListFocus = pFocus;
        lList = lListOwner;
        pListEmptyNotice = PListNoticeBuild();
        PListStackBody = PListBodyBuild();
    }

    public UIElement PListStackBody { get; }

    private static TextBlock PListNoticeBuild() => new()
    {
        Text = QLocalizationCatalog.QLocalizationTextRead("List.Empty.Notice"),
        FontSize = 12,
        FontFamily = pListFontFamily,
        Foreground = pListMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(16, 24, 16, 16)
    };

    private Grid PListBodyBuild()
    {
        var pBody = new Grid();
        pBody.Children.Add(pListEmptyNotice);
        pBody.Children.Add(pListRowPanel);
        return pBody;
    }

    public void PListRowsRebuild()
    {
        PListPanelRebuild();
        PListNoticeApply();
    }

    private void PListPanelRebuild()
    {
        pListRowPanel.Children.Clear();
        pListRows.Clear();
        lList.QListFace.QListCardsRead()
            .Select(PListCardBuild)
            .ToList()
            .ForEach(pCard => pListRowPanel.Children.Add(pCard));
    }

    private void PListNoticeApply() => pListEmptyNotice.Visibility = PLook.PLookVisible[lList.QListEmpty];

    private UIElement PListCardBuild(QListCard lCard) => pListCards[lCard.QListCardLocked](this, lCard);

    private Border PListRowBuild(QListRow lRow)
    {
        var pRow = new PListRow(pListFocus, lList, lRow);
        pListRows.Add(pRow);
        return pRow.PListRowBorder;
    }

    private UIElement PListLockedBuild(QListCard lCard)
    {
        var pListCardRows = new StackPanel();
        var pListHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 7, 10, 5)
        };
        pListHeader.Children.Add(new TextBlock
        {
            Text = "",
            FontSize = 11,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });
        pListHeader.Children.Add(new TextBlock
        {
            Text = lCard.QListCardTitle,
            FontSize = 11,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pListCardRows.Children.Add(pListHeader);
        lCard.QListCardRows.Select(PListRowBuild).ToList().ForEach(pRow => pListCardRows.Children.Add(pRow));

        return new Border
        {
            Margin = new Thickness(6, 6, 6, 0),
            Background = pListLockedBrush,
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(0, 0, 0, 4),
            Child = pListCardRows
        };
    }

    public void PListSelectionUpdate(string? lPath) => pListRows.ForEach(PListRowUpdate);

    private void PListRowUpdate(PListRow pRow) =>
        pRow.PListStateApply(lList.QListFace.QListStateRead(pRow.PListRowPath));
}
