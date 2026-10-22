using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QExportName(
    Func<string?> selection,
    Action<string> select,
    Action<string, string> commit,
    Action repaint,
    Action sync)
{
    private (TextBox, Action, Func<bool>)? qExportName;

    public string? QExportNameEditing { get; private set; }

    public bool QExportNameCheck(string lPresetName) =>
        string.Equals(lPresetName, QExportNameEditing, StringComparison.OrdinalIgnoreCase);

    public void QExportNameStart(string lPresetName)
    {
        if (!string.Equals(lPresetName, selection(), StringComparison.OrdinalIgnoreCase))
        {
            select(lPresetName);
        }

        QExportNameEditing = lPresetName;
        repaint();
    }

    public void QExportNameCancel()
    {
        QExportNameDetach();
        QExportNameEditing = null;
        sync();
    }

    public bool QExportNameCommit(string lOldName, string lNewName)
    {
        if (!QExportNameCheck(lOldName))
        {
            return false;
        }

        QExportNameDetach();
        QExportNameEditing = null;
        commit(lOldName, lNewName);
        sync();
        return true;
    }

    public bool QExportNameCommit()
    {
        return qExportName?.Item3() ?? false;
    }

    public void QExportNameAttach(TextBox pNameBox, string lPresetName)
    {
        QExportNameDetach();
        if (!QExportNameCheck(lPresetName))
        {
            return;
        }

        Window? window = null;
        MouseButtonEventHandler outside = (_, pEvent) =>
        {
            if (qExportName is not { } session || !ReferenceEquals(session.Item1, pNameBox))
            {
                return;
            }

            DependencyObject? source = pEvent.OriginalSource as DependencyObject;
            while (source is not null)
            {
                if (ReferenceEquals(source, pNameBox))
                {
                    return;
                }

                source = source is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            QExportNameCommit();
        };
        RoutedEventHandler loaded = (_, _) =>
        {
            if (qExportName is not { } session || !ReferenceEquals(session.Item1, pNameBox))
            {
                return;
            }

            if (window is not null)
            {
                window.PreviewMouseDown -= outside;
            }

            pNameBox.Focus();
            pNameBox.SelectAll();
            window = Window.GetWindow(pNameBox);
            if (window is not null)
            {
                window.PreviewMouseDown += outside;
            }
        };
        RoutedEventHandler unloaded = (_, _) =>
        {
            if (qExportName is { } session && ReferenceEquals(session.Item1, pNameBox))
            {
                QExportNameDetach();
            }
        };
        KeyboardFocusChangedEventHandler focus = (_, _) =>
        {
            if (qExportName is { } session && ReferenceEquals(session.Item1, pNameBox))
            {
                QExportNameCommit();
            }
        };
        KeyEventHandler key = (_, pEvent) =>
        {
            if (qExportName is not { } session || !ReferenceEquals(session.Item1, pNameBox))
            {
                return;
            }

            if (pEvent.Key == Key.Return)
            {
                QExportNameCommit();
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                QExportNameCancel();
                pEvent.Handled = true;
            }
        };

        Action detach = () =>
        {
            pNameBox.Loaded -= loaded;
            pNameBox.Unloaded -= unloaded;
            pNameBox.LostKeyboardFocus -= focus;
            pNameBox.KeyDown -= key;
            if (window is not null)
            {
                window.PreviewMouseDown -= outside;
            }
        };
        qExportName = (pNameBox, detach, () => QExportNameCommit(lPresetName, pNameBox.Text));
        pNameBox.Loaded += loaded;
        pNameBox.Unloaded += unloaded;
        pNameBox.LostKeyboardFocus += focus;
        pNameBox.KeyDown += key;
    }

    public void QExportNameDetach()
    {
        (TextBox, Action, Func<bool>)? session = qExportName;
        qExportName = null;
        session?.Item2();
    }

    public void QExportNameDetach(TextBox pNameBox)
    {
        if (qExportName is { } session && ReferenceEquals(session.Item1, pNameBox))
        {
            QExportNameDetach();
        }
    }

    public void QExportNameSync(string? lPresetName)
    {
        if (!string.Equals(QExportNameEditing, lPresetName, StringComparison.OrdinalIgnoreCase))
        {
            QExportNameDetach();
            QExportNameEditing = null;
        }
    }
}
