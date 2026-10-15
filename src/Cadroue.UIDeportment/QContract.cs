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
        return QContractResolve<QContractPart>(lEntry, lName, "element");
    }

    public static QContractSheet QContractSheetFind<QContractSheet>(string lName)
        where QContractSheet : class
    {
        ArgumentException.ThrowIfNullOrEmpty(lName);

        object? lEntry = System.Windows.Application.Current?.TryFindResource(lName);
        return QContractResolve<QContractSheet>(lEntry, lName, "resource");
    }

    public static QContractSheet QContractSheetFind<QContractSheet>(FrameworkElement lSurface, string lName)
        where QContractSheet : class
    {
        ArgumentNullException.ThrowIfNull(lSurface);
        ArgumentException.ThrowIfNullOrEmpty(lName);

        object? lEntry = lSurface.TryFindResource(lName);
        return QContractResolve<QContractSheet>(lEntry, lName, "resource");
    }

    private static QContractPart QContractResolve<QContractPart>(object? lEntry, string lName, string lContract)
        where QContractPart : class
    {
        return lEntry as QContractPart ?? throw new InvalidOperationException(
            $"The contract {lContract} '{lName}' is missing or is not a {typeof(QContractPart).Name}.");
    }
}
