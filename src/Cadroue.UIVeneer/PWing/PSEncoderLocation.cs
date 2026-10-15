using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
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

    private const string PSLocationBrowseIcon = "/PAsset/PPanel/POpen.svg";

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
        string pCurrent = psLocationFolderBox.Text.Trim();
        var pDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Choose"),
            InitialDirectory = lsEncoder.QSEncoderInitialResolve(pCurrent)
        };
        if (pDialog.ShowDialog() == true)
        {
            psLocationFolderBox.Text = pDialog.FolderName;
        }
    }

    private void PSLocationModeUpdate()
    {
        string pMode = PSField.PSModeTextRead(psLocationMode);

        if (psLocationFolderBox is not null)
        {
            psLocationFolderBox.Text = lsEncoder.QSEncoderLocationSelect(pMode, psLocationFolderBox.Text);
        }

        bool pFolder = QSEncoder.QSEncoderFolderCheck(pMode);

        if (psLocationFolderRow is not null)
        {
            psLocationFolderRow.Visibility = pFolder ? Visibility.Visible : Visibility.Collapsed;
        }

        if (psLocationBrowse is not null)
        {
            psLocationBrowse.Visibility = QSEncoder.QSEncoderCustomCheck(pMode)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (psLocationFolderLabel is not null)
        {
            psLocationFolderLabel.Text = QLocalizationCatalog.QLocalizationTextRead(QSEncoder.QSEncoderFolderResolve(
                pMode));
        }

        if (psLocationStatus is not null)
        {
            psLocationStatus.Text = QLocalizationCatalog.QLocalizationTextRead(QSEncoder.QSEncoderStatusResolve(pMode));
        }
    }
}
