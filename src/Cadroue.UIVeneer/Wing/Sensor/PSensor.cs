using System;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

public sealed class PSensor : ContentControl
{
    public PSensor()
    {
        var pSheet = new ResourceDictionary
        {
            Source = new Uri("/Cadroue.UIVeneer;component/Wing/Sensor/PSensor.xaml", UriKind.RelativeOrAbsolute)
        };
        PSensorBody = (StackPanel)pSheet[nameof(PSensorBody)];
        Content = PSensorBody;
    }

    public StackPanel PSensorBody { get; }
}
