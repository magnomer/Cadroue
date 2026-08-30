using Cadroue.Core;
using Cadroue.UIShell.PSShared;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PAssets;
using Cadroue.Media;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed class PSource : UserControl
{
    private static readonly SolidColorBrush PSourceTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush PSourceMutedBrush = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush PSourceBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private const double PSourceRowHeight = 38;
    private const double PSourceBrowseSize = 18;
    private PViewer? pSourceViewer;
    private readonly bool pSourceAudioAllowed;
    private readonly TextBox pSourcePathBox;
    private readonly TextBlock pSourcePlaceholderText;

    public PSource(bool pAudioOnlyAllowed)
    {
        pSourceAudioAllowed = pAudioOnlyAllowed;
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
            Text = LLocalization.LLocalizationTextRead("Source.Empty.Notice"),
            FontSize = 11,
            Foreground = PSourceMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        var pPathIcon = PSourceIconCreate();
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
    }

    public void PSourceAttach(PViewer? pViewer)
    {
        if (pSourceViewer is not null)
            pSourceViewer.PViewerMediaChange -= PSourceMediaHandle;
        pSourceViewer = pViewer;
        if (pSourceViewer is not null)
            pSourceViewer.PViewerMediaChange += PSourceMediaHandle;
    }

    private void PSourceMediaHandle(LCargo pMediaStatus)
    {
        pSourcePathBox.Text = pMediaStatus.LCargoSourcePath;
    }

    private void PSourceTextHandle(object sender, TextChangedEventArgs e)
    {
        PSourcePlaceholderSync();
    }


    private void PSourceOpenHandle(object sender, RoutedEventArgs e)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Source.Dialog.Open"),
            Filter = PSourceFilterRead()
        };
        if (pDialog.ShowDialog() != true) return;
        if (Cadroue.Media.LMedia.LMediaAudioCheck(pDialog.FileName) && !pSourceAudioAllowed)
        {
            PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Title"), LLocalization.LLocalizationTextRead("Source.AudioOnly.Message"));
            return;
        }

        pSourceViewer?.PViewerSourceOpen(pDialog.FileName);
    }

    private string PSourceFilterRead()
    {
        string pVideoPattern = PSourcePatternRead(Cadroue.Media.LMedia.LMediaVideoExtensions);
        string pAudioPattern = PSourcePatternRead(Cadroue.Media.LMedia.LMediaAudioExtensions);
        const string pSidecarPattern = "*.cad";
        return pSourceAudioAllowed
            ? LLocalization.LLocalizationFormat("Source.Dialog.MediaProjectFilter", pVideoPattern, pAudioPattern, pSidecarPattern)
            : LLocalization.LLocalizationFormat("Source.Dialog.VideoProjectFilter", pVideoPattern, pSidecarPattern);
    }

    private static string PSourcePatternRead(IReadOnlyList<string> pSourceExtensions) =>
        string.Join(";", pSourceExtensions.Select(pSourceExtension => $"*{pSourceExtension}"));

    private void PSourceKeyHandle(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return) return;
        string pPath = pSourcePathBox.Text.Trim();
        if (!File.Exists(pPath)) return;
        if (Cadroue.Media.LMedia.LMediaAudioCheck(pPath) && !pSourceAudioAllowed)
        {
            PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Title"), LLocalization.LLocalizationTextRead("Source.AudioOnly.Message"));
            return;
        }

        pSourceViewer?.PViewerSourceOpen(pPath);
        e.Handled = true;
    }

    private void PSourcePlaceholderSync()
    {
        pSourcePlaceholderText.Visibility = string.IsNullOrWhiteSpace(pSourcePathBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static Image PSourceIconCreate()
    {
        return new Image
        {
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 0, 10, 0),
            Stretch = Stretch.Uniform,
            Source = PIcon.PIconRead("/PAssets/PPanels/PVideo.svg")
        };
    }

    private static Image PSourceBrowseCreate()
    {
        return new Image
        {
            Width = PSourceBrowseSize,
            Height = PSourceBrowseSize,
            Stretch = Stretch.Uniform,
            Source = PIcon.PIconRead("/PAssets/PPanels/PFolder.svg")
        };
    }

}
