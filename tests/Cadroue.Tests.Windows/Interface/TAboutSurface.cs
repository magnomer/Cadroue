using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TProgramCollection))]
public sealed class TAboutSurface
{
    [Fact]
    public void Surface_EveryNamedPart_Resolves()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                _ = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var surface = new PSAbout();
                QContract.QContractFind<DockPanel>(surface, "PSAboutBody");
                QContract.QContractFind<Image>(surface, "PSAboutMark");
                QContract.QContractFind<TextBlock>(surface, "PSAboutDeveloper");
                QContract.QContractFind<TextBlock>(surface, "PSAboutBrand");
                QContract.QContractFind<TextBlock>(surface, "PSAboutVersion");
                QContract.QContractFind<Hyperlink>(surface, "PSAboutProject");
                QContract.QContractFind<TextBlock>(surface, "PSAboutTitle");
                QContract.QContractFind<StackPanel>(surface, "PSAboutGallery");
                QContract.QContractFind<TextBlock>(surface, "PSAboutLicense");
                QContract.QContractFind<Hyperlink>(surface, "PSAboutDocument");
                QContract.QContractFind<Button>(surface, "PSAboutCloser");
                var row = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(surface, "PSAboutCredit")
                    .LoadContent();
                QContract.QContractFind<TextBlock>(row, "PSAboutLabel");
                QContract.QContractFind<Hyperlink>(row, "PSAboutLink");
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
