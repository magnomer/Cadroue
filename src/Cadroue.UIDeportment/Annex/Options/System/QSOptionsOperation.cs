using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsOperation
{
    private readonly Button psOptionsInstaller;
    private readonly ProgressBar psOptionsProgress;

    public QSOptionsOperation(Button installer, ProgressBar progress)
    {
        psOptionsInstaller = installer;
        psOptionsProgress = progress;
    }

    public void QSOptionsOperationStart()
    {
        psOptionsInstaller.IsEnabled = false;
        psOptionsProgress.Value = 0;
        psOptionsProgress.Visibility = Visibility.Visible;
    }

    public void QSOptionsOperationHide() => psOptionsProgress.Visibility = Visibility.Collapsed;

    public void QSOptionsOperationApply(double value) => psOptionsProgress.Value = value;

    public void QSOptionsOperationApply(Action<Button> apply) => apply(psOptionsInstaller);
}
