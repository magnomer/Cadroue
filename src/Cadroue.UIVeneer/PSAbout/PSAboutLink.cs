using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAboutLink
{
    private readonly QSAbout lsAbout;

    internal PSAboutLink(QSAbout lsAboutOwner) => lsAbout = lsAboutOwner;

    internal TextBlock PSAboutLinkBuild(string pUrl, HorizontalAlignment pAlignment)
    {
        var pLink = new Hyperlink(new Run(pUrl)) { NavigateUri = new Uri(pUrl) };
        PSAboutLinkAttach(pLink);

        var pText = new TextBlock
        {
            HorizontalAlignment = pAlignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        pText.Inlines.Add(pLink);
        return pText;
    }

    private void PSAboutLinkAttach(Hyperlink pLink) => pLink.RequestNavigate += PSAboutLinkHandle;

    private void PSAboutLinkHandle(object pSender, RequestNavigateEventArgs pEvent)
    {
        pEvent.Handled = true;
        lsAbout.QSAboutLinkOpen(pEvent.Uri.AbsoluteUri);
    }
}
