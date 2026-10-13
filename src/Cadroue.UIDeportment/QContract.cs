using System.Windows;

namespace Cadroue.UIDeportment;

public static class QContract
{
    public static QContractPart QContractFind<QContractPart>(DependencyObject lSurface, string lName)
        where QContractPart : class
    {
        ArgumentNullException.ThrowIfNull(lSurface);
        ArgumentException.ThrowIfNullOrEmpty(lName);

        object? lEntry = (lSurface as FrameworkElement)?.FindName(lName);
        lEntry ??= LogicalTreeHelper.FindLogicalNode(lSurface, lName);
        return lEntry as QContractPart ?? throw new InvalidOperationException(
            $"The contract element '{lName}' is missing or is not a {typeof(QContractPart).Name}.");
    }
}
