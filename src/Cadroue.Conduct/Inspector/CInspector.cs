namespace Cadroue.Conduct;

// The inspector's gate: one decision object per tool, each exposed as one property. CAtelier makes one per inspector.
public sealed class CInspector
{
    internal CInspector()
    {
    }

    public CGamma CInspectorGamma { get; } = new();
}
