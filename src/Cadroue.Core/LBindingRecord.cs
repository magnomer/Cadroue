namespace Cadroue.Core;

public sealed class LBindingRecord
{
    public string LBindingRecordToken { get; set; } = string.Empty;

    public string LBindingRecordGesture { get; set; } = string.Empty;

    public LBindingRecord LBindingRecordClone() => new()
    {
        LBindingRecordToken = LBindingRecordToken,
        LBindingRecordGesture = LBindingRecordGesture
    };
}
