using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAboutHeader
{
    private const string PSAboutLogoPath = "/PAsset/PProgram/PProgramIcon.png";
    private const double PSAboutLogoSize = 76;

    private readonly QSAbout lsAbout;
    private readonly PSAboutLink psAboutLink;

    private TextBlock? psAboutDeveloper;

    internal PSAboutHeader(QSAbout lsAboutOwner)
    {
        lsAbout = lsAboutOwner;
        psAboutLink = new PSAboutLink(lsAboutOwner);
    }

    internal UIElement PSAboutHeaderBuild()
    {
        var pHeader = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 26)
        };

        var pLogo = new Image
        {
            Source = QIcon.QIconRead(PSAboutLogoPath, PSAboutLogoSize),
            Width = PSAboutLogoSize,
            Height = PSAboutLogoSize,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 0, 12)
        };
        PSAboutLogoAttach(pLogo);
        pHeader.Children.Add(pLogo);

        psAboutDeveloper = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("About.Developer.Active"),
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            Visibility = PSAboutDeveloperRead()
        };
        pHeader.Children.Add(psAboutDeveloper);

        pHeader.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Terms.Cadroue"),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pHeader.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationFormat("About.Version.Format", QSAbout.QSAboutVersionRead()),
            Foreground = PSField.PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 10)
        });
        pHeader.Children.Add(psAboutLink.PSAboutLinkBuild(QSAbout.QSAboutProjectUrl, HorizontalAlignment.Center));
        return pHeader;
    }

    private void PSAboutLogoAttach(Image pLogo) => pLogo.MouseLeftButtonUp += PSAboutTapHandle;

    private Visibility PSAboutDeveloperRead() =>
        lsAbout.QSAboutTap.QSAboutDeveloper ? Visibility.Visible : Visibility.Collapsed;

    private void PSAboutTapHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        if (lsAbout.QSAboutTap.QSAboutTapChange(DateTime.UtcNow) && psAboutDeveloper is not null)
        {
            PSAboutDeveloperShow(psAboutDeveloper);
        }
    }

    private void PSAboutDeveloperShow(TextBlock pDeveloper) => pDeveloper.Visibility = PSAboutDeveloperRead();
}
