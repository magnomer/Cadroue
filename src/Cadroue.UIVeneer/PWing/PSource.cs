using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Microsoft.Win32;

namespace Cadroue.UIVeneer.PWing;

public sealed class PSource : UserControl
{
    private static readonly SolidColorBrush PSourceTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush PSourceMutedBrush = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush PSourceBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private const double PSourceRowHeight = 38;
    private const double PSourceBrowseSize = 18;
    private readonly TextBox pSourcePathBox;
    private readonly TextBlock pSourcePlaceholderText;

    public LSource LSource { get; }

    public PSource(LSource lSource)
    {
        LSource = lSource;
        MinHeight = PSourceRowHeight;

        pSourcePathBox = new TextBox
        {
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = PSourceTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            FocusVisualStyle = null
        };
        pSourcePathBox.KeyDown += PSourceKeyHandle;
        pSourcePathBox.TextChanged += PSourceTextHandle;

        pSourcePlaceholderText = new TextBlock
        {
            Text = LSource.LSourcePlaceholderRead(),
            FontSize = 11,
            Foreground = PSourceMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        Image pPathIcon = PSourceIconCreate();
        var pPathContent = new Grid();
        pPathContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pPathContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pPathIcon, 0);
        Grid.SetColumn(pSourcePathBox, 1);
        Grid.SetColumn(pSourcePlaceholderText, 1);
        pPathContent.Children.Add(pPathIcon);
        pPathContent.Children.Add(pSourcePathBox);
        pPathContent.Children.Add(pSourcePlaceholderText);

        var pPathBorder = new Border
        {
            MinHeight = PSourceRowHeight,
            Padding = new Thickness(14, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderBrush = PSourceBorderBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Child = pPathContent
        };

        var pBrowseButton = new Button
        {
            Content = PSourceBrowseCreate(),
            Style = PButton.PButtonSourceCreate()
        };
        pBrowseButton.Click += PSourceOpenHandle;

        var pRow = new Grid { Margin = new Thickness(16, 6, 16, 4) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pPathBorder, 0);
        Grid.SetColumn(pBrowseButton, 2);
        pRow.Children.Add(pPathBorder);
        pRow.Children.Add(pBrowseButton);

        Content = pRow;

        PSourcePlaceholderSync();
        LSource.LSourcePathApply += PSourcePathApply;
        LSource.LSourceRefuse += PSourceRefuseShow;
    }

    private void PSourcePathApply(string lPath) => pSourcePathBox.Text = lPath;

    private void PSourceTextHandle(object pSender, TextChangedEventArgs pEvent) => PSourcePlaceholderSync();

    private void PSourceOpenHandle(object pSender, RoutedEventArgs pEvent)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LSource.LSourceTitleRead(),
            Filter = LSource.LSourceFilterRead()
        };
        LSource.LSourceDialogOpen(pDialog.ShowDialog(), pDialog.FileName);
    }

    private void PSourceRefuseShow(string lTitle, string lMessage) =>
        PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this), lTitle, lMessage);

    private void PSourceKeyHandle(object pSender, KeyEventArgs pEvent) =>
        pEvent.Handled = LSource.LSourceKeyRun(pEvent.Key.ToString(), pSourcePathBox.Text);

    private void PSourcePlaceholderSync() =>
        pSourcePlaceholderText.Visibility = PLook.PLookVisible[LSource.LSourcePlaceholderCheck(pSourcePathBox.Text)];

    private static Image PSourceIconCreate() => new()
    {
        Width = 20,
        Height = 20,
        Margin = new Thickness(0, 0, 10, 0),
        Stretch = Stretch.Uniform,
        Source = PIcon.PIconRead("/PAsset/PPanel/PVideo.svg")
    };

    private static Image PSourceBrowseCreate() => new()
    {
        Width = PSourceBrowseSize,
        Height = PSourceBrowseSize,
        Stretch = Stretch.Uniform,
        Source = PIcon.PIconRead("/PAsset/PPanel/PFolder.svg")
    };
}
