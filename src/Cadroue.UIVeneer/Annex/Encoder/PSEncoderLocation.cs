using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSEncoderLocation
{
    private const string PSLocationBrowseIcon = "/PAsset/PPanel/POpen.svg";

    private readonly QSEncoderLocation lsEncoderLocation;
    private readonly Border psLocationMode;
    private readonly TextBox psLocationFolderBox;
    private UIElement? psLocationFolderRow;
    private TextBlock? psLocationFolderLabel;
    private Button? psLocationBrowse;
    private TextBlock? psLocationStatus;

    internal PSEncoderLocation(QSEncoderLocation lsLocation, LPreset lsDraft)
    {
        lsEncoderLocation = lsLocation;
        psLocationMode = PSMode.PSModeBuild(lsDraft.LPresetLocation, PSLocationModeUpdate,
            new LLocalizationChoice("Same as source", "Encoder.Location.SourceFolder"),
            new LLocalizationChoice("Subfolder", "Encoder.Location.Subfolder"),
            new LLocalizationChoice("Sibling", "Encoder.Location.Sibling"),
            new LLocalizationChoice("Custom location", "Encoder.Location.Custom"));
        psLocationFolderBox = PSEntry.PSEntryBuild(
            lsDraft.LPresetLocationRead(lsDraft.LPresetLocation),
            220);
    }

    internal string PSLocationModeRead() => PSMode.PSModeTextRead(psLocationMode);

    internal string PSLocationFolderRead() => PSEncoderValue.PSEncoderTrimRead(psLocationFolderBox);

    internal void PSLocationBuild(Panel pHost)
    {
        psLocationStatus = new TextBlock
        {
            Foreground = PSField.PSFieldMuted,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        psLocationFolderLabel = PSField.PSFieldLabelBuild(string.Empty);
        psLocationBrowse = PSInline.PSInlineIconBuild(
            PSLocationBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Browse"),
            new Thickness(8, 0, 0, 0));
        psLocationFolderRow = PSFieldLabelledBuild(psLocationFolderLabel, psLocationFolderBox, psLocationBrowse);
        pHost.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pHost.Children.Add(psLocationFolderRow);
    }

    internal void PSLocationAttach() => psLocationBrowse!.Click += (_, _) => PSLocationCustomRead();

    private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(
            PSField.PSFieldLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Location")));

        var pValueGrid = new Grid();
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(psLocationMode, 0);
        Grid.SetColumn(psLocationStatus, 1);
        pValueGrid.Children.Add(psLocationMode);
        pValueGrid.Children.Add(psLocationStatus);

        Grid.SetColumn(pValueGrid, 1);
        pGrid.Children.Add(pValueGrid);
        return pGrid;
    }

    private static UIElement PSFieldLabelledBuild(TextBlock pLabel, TextBox pBox, Button pBrowse)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSField.PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);

        var pValueRow = new StackPanel { Orientation = Orientation.Horizontal };
        pBox.MinHeight = PSField.PSFieldControlHeight;
        pValueRow.Children.Add(pBox);
        pValueRow.Children.Add(pBrowse);

        Grid.SetColumn(pValueRow, 1);
        pGrid.Children.Add(pValueRow);
        return pGrid;
    }

    private void PSLocationCustomRead()
    {
        string pCurrent = PSLocationCurrentRead();
        string? pFolder = PSLocationDialogShow(pCurrent);
        if (pFolder is not null)
        {
            PSLocationFolderSet(pFolder);
        }
    }

    private string PSLocationCurrentRead() => psLocationFolderBox.Text.Trim();

    private string? PSLocationDialogShow(string pCurrent)
    {
        var pDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Choose"),
            InitialDirectory = lsEncoderLocation.QSEncoderInitialResolve(pCurrent)
        };
        return pDialog.ShowDialog() == true ? pDialog.FolderName : null;
    }

    private void PSLocationFolderSet(string pFolder) => psLocationFolderBox.Text = pFolder;

    internal void PSLocationModeUpdate()
    {
        string pMode = PSMode.PSModeTextRead(psLocationMode);

        if (psLocationFolderBox is not null)
        {
            psLocationFolderBox.Text =
                lsEncoderLocation.QSEncoderLocationSelect(pMode, psLocationFolderBox.Text);
        }

        bool pFolder = QSEncoderLocation.QSEncoderFolderCheck(pMode);

        if (psLocationFolderRow is not null)
        {
            psLocationFolderRow.Visibility = pFolder ? Visibility.Visible : Visibility.Collapsed;
        }

        if (psLocationBrowse is not null)
        {
            psLocationBrowse.Visibility = QSEncoderLocation.QSEncoderCustomCheck(pMode)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (psLocationFolderLabel is not null)
        {
            psLocationFolderLabel.Text = QLocalizationCatalog.QLocalizationTextRead(
                QSEncoderLocation.QSEncoderFolderResolve(pMode));
        }

        if (psLocationStatus is not null)
        {
            psLocationStatus.Text = QLocalizationCatalog.QLocalizationTextRead(
                QSEncoderLocation.QSEncoderStatusResolve(pMode));
        }
    }
}
