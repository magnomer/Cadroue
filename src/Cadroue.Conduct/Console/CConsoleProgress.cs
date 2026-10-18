using System;

namespace Cadroue.Conduct;

// The progress a console's bar shows: a fraction held inside 0 to 1, and whether the bar glides to it. CConsole makes
// one and sets it from the shown station's running records.
public sealed class CConsoleProgress
{
    private double _cConsoleProgressValue;
    private bool _cConsoleProgressBackward;

    public event Action? CConsoleProgressChanged;

    public double CConsoleProgressValue => _cConsoleProgressValue;

    // The bar glides to a forward fraction above zero and jumps otherwise.
    public bool CConsoleProgressGlide => _cConsoleProgressValue > 0 && !_cConsoleProgressBackward;

    // Only a change is told.
    internal void CConsoleProgressSet(double target)
    {
        double clamped = Math.Clamp(target, 0, 1);
        if (clamped.Equals(_cConsoleProgressValue))
        {
            return;
        }

        _cConsoleProgressBackward = clamped < _cConsoleProgressValue;
        _cConsoleProgressValue = clamped;
        CConsoleProgressChanged?.Invoke();
    }
}
