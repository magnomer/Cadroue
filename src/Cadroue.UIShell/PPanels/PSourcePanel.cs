using Cadroue.UIShell.PMainWindow;
using Cadroue.Media;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PPanels;

public sealed class PSourcePanel : UserControl
{
    private static readonly SolidColorBrush PSourcePanelTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush PSourcePanelBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PSourcePanelIconBrush = new(Color.FromRgb(0x5B, 0x63, 0x71));
    private PViewerPanel? pSourcePanelViewer;
    private readonly bool pSourcePanelAudioOnlyAllowed;
    private readonly TextBox pSourcePathBox;

    public PSourcePanel(bool pAudioOnlyAllowed)
    {
        pSourcePanelAudioOnlyAllowed = pAudioOnlyAllowed;
        MinHeight = 50;

        pSourcePathBox = new TextBox
        {
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = PSourcePanelTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            FocusVisualStyle = null
        };
        pSourcePathBox.KeyDown += PSourcePathBoxKeyDownHandle;

        var pPathIcon = PSourcePanelFolderIconCreate();
        var pPathContent = new Grid();
        pPathContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pPathContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pPathIcon, 0);
        Grid.SetColumn(pSourcePathBox, 1);
        pPathContent.Children.Add(pPathIcon);
        pPathContent.Children.Add(pSourcePathBox);

        var pPathBorder = new Border
        {
            MinHeight = 38,
            Padding = new Thickness(14, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderBrush = PSourcePanelBorderBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Child = pPathContent
        };

        var pBrowseButton = new Button
        {
            Content = "Browse",
            Width = 108,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PMainButton.PButtonNormalGreyCreate()
        };
        pBrowseButton.Click += PSourceOpenButtonClickHandle;

        var pRow = new Grid { Margin = new Thickness(16, 8, 16, 8) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pPathBorder, 0);
        Grid.SetColumn(pBrowseButton, 1);
        pRow.Children.Add(pPathBorder);
        pRow.Children.Add(pBrowseButton);

        Content = pRow;
    }

    public void PSourcePanelAttach(PViewerPanel? pViewer)
    {
        if (pSourcePanelViewer is not null)
            pSourcePanelViewer.PViewerPanelMediaStatusChange -= PSourcePanelMediaStatusChangeHandle;
        pSourcePanelViewer = pViewer;
        if (pSourcePanelViewer is not null)
            pSourcePanelViewer.PViewerPanelMediaStatusChange += PSourcePanelMediaStatusChangeHandle;
    }

    private void PSourcePanelMediaStatusChangeHandle(LMediaOpenStatus pMediaStatus)
    {
        pSourcePathBox.Text = pMediaStatus.LMediaOpenSourcePath;
    }

    private void PSourceOpenButtonClickHandle(object sender, RoutedEventArgs e)
    {
        var pDialog = new OpenFileDialog
        {
            Title = "Open media file",
            Filter = PSourcePanelOpenFilterRead()
        };
        if (pDialog.ShowDialog() != true) return;
        if (PSourcePanelAudioExtensionCheck(pDialog.FileName) && !pSourcePanelAudioOnlyAllowed)
        {
            MessageBox.Show("Audio-only files can be opened only in the Audio tab.", "Cannot open file",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        pSourcePanelViewer?.PViewerPanelSourceOpenRequest(pDialog.FileName);
    }

    private string PSourcePanelOpenFilterRead()
    {
        const string pVideoPattern = "*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.ts;*.mts;*.m2ts";
        const string pAudioPattern = "*.mp3;*.aac;*.flac;*.wav;*.ogg";
        return pSourcePanelAudioOnlyAllowed
            ? $"Media files|{pVideoPattern};{pAudioPattern}|All files|*.*"
            : $"Video files|{pVideoPattern}|All files|*.*";
    }

    private static bool PSourcePanelAudioExtensionCheck(string pSourcePath)
    {
        string pExtension = System.IO.Path.GetExtension(pSourcePath);
        return pExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
    }

    private void PSourcePathBoxKeyDownHandle(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return) return;
        string pPath = pSourcePathBox.Text.Trim();
        if (!File.Exists(pPath)) return;
        if (PSourcePanelAudioExtensionCheck(pPath) && !pSourcePanelAudioOnlyAllowed)
        {
            MessageBox.Show("Audio-only files can be opened only in the Audio tab.", "Cannot open file",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        pSourcePanelViewer?.PViewerPanelSourceOpenRequest(pPath);
        e.Handled = true;
    }

    private static Viewbox PSourcePanelFolderIconCreate()
    {
        var pCanvas = new Canvas { Width = 28, Height = 28 };

        var pCard = new Rectangle
        {
            Width = 21,
            Height = 15.5,
            RadiusX = 2.3,
            RadiusY = 2.3,
            Stroke = PSourcePanelIconBrush,
            StrokeThickness = 2.0,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(pCard, 3.5);
        Canvas.SetTop(pCard, 6.0);
        pCanvas.Children.Add(pCard);

        var pPlay = new Polygon
        {
            Fill = PSourcePanelIconBrush,
            Points = new PointCollection(new[]
            {
                new Point(11.0, 10.4),
                new Point(11.0, 17.0),
                new Point(16.5, 13.7)
            })
        };
        pCanvas.Children.Add(pPlay);

        return new Viewbox
        {
            Width = 28,
            Height = 28,
            Margin = new Thickness(0, 0, 12, 0),
            Stretch = Stretch.Uniform,
            Child = pCanvas
        };
    }

}
