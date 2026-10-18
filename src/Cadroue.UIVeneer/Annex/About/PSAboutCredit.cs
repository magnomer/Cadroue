using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAboutCredit
{
    private const double PSAboutRowGap = 7;

    private readonly QSAbout lsAbout;
    private readonly PSAboutLink psAboutLink;

    internal PSAboutCredit(QSAbout lsAboutOwner)
    {
        lsAbout = lsAboutOwner;
        psAboutLink = new PSAboutLink(lsAboutOwner);
    }

    internal UIElement PSAboutCreditBuild()
    {
        var pRows = new List<UIElement>(QSAbout.QSAboutCredits.Count + 1);
        foreach ((string pName, string pUrl) in QSAbout.QSAboutCredits)
        {
            pRows.Add(PSAboutRowBuild(pName, pUrl));
        }

        pRows.Add(PSAboutLicenseBuild());
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("About.Credits.Title"), pRows.ToArray());
    }

    private UIElement PSAboutRowBuild(string pName, string pUrl)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSField.PSFieldLabelBuild(pName));

        TextBlock pLink = psAboutLink.PSAboutLinkBuild(pUrl, HorizontalAlignment.Left);
        Grid.SetColumn(pLink, 1);
        pRow.Children.Add(pLink);
        return pRow;
    }

    private UIElement PSAboutLicenseBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSField.PSFieldLabelBuild(QLocalizationCatalog.QLocalizationTextRead(
            "About.Licenses.Label")));

        var pLink = new Hyperlink(new Run(QLocalizationCatalog.QLocalizationTextRead("About.Licenses.Link")));
        PSAboutNoticeAttach(pLink);

        var pText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        pText.Inlines.Add(pLink);
        Grid.SetColumn(pText, 1);
        pRow.Children.Add(pText);
        return pRow;
    }

    private void PSAboutNoticeAttach(Hyperlink pLink) => pLink.Click += PSAboutNoticeHandle;

    private void PSAboutNoticeHandle(object pSender, RoutedEventArgs pEvent)
    {
        pEvent.Handled = true;
        lsAbout.QSAboutNoticeOpen();
    }
}
