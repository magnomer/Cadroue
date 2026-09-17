using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSCombo;
using static Cadroue.UIVeneer.PSCasement.PSInline;
using static Cadroue.UIVeneer.PSCasement.PSPlate;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Location")));

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
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);

        var pValueRow = new StackPanel { Orientation = Orientation.Horizontal };
        pBox.MinHeight = PSFieldControlHeight;
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
            Title = LLocalization.LLocalizationTextRead("Encoder.Location.Choose"),
            InitialDirectory = LUsher.LUsherFolderExist(pCurrent) ? pCurrent : string.Empty
        };
        if (pDialog.ShowDialog() == true)
        {
            psLocationFolderBox.Text = pDialog.FolderName;
        }
    }

    private void PSLocationModeUpdate()
    {
        string pMode = PSModeTextRead(psLocationMode);

        if (psLocationFolderBox is not null)
        {
            psLocationFolderBox.Text = lsEncoder.LSEncoderLocationSelect(pMode, psLocationFolderBox.Text);
        }

        bool pFolder = LSEncoder.LSEncoderFolderCheck(pMode);

        if (psLocationFolderRow is not null)
        {
            psLocationFolderRow.Visibility = pFolder ? Visibility.Visible : Visibility.Collapsed;
        }

        if (psLocationBrowse is not null)
        {
            psLocationBrowse.Visibility = LSEncoder.LSEncoderCustomCheck(pMode)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (psLocationFolderLabel is not null)
        {
            psLocationFolderLabel.Text = LLocalization.LLocalizationTextRead(LSEncoder.LSEncoderFolderResolve(pMode));
        }

        if (psLocationStatus is not null)
        {
            psLocationStatus.Text = LLocalization.LLocalizationTextRead(LSEncoder.LSEncoderStatusResolve(pMode));
        }
    }
}
