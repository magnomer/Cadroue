using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TKeymapChord
{
    [Fact]
    public void Capture_PendingBecomesGesture_OnCommit()
    {
        LSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        LSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        int notices = 0;
        TInterface.TKeymapAttach(keymap, _ => notices++);
        string gesture = TInterface.TBindingGestureFormat("F9", true, false, true, false);

        TInterface.TKeymapPendingSet(keymap, chord, gesture);
        Assert.Equal(string.Empty, chord.LSKeymapChordPending);

        TInterface.TKeymapChordStart(keymap, chord);
        Assert.True(chord.LSKeymapChordActive);
        TInterface.TKeymapPendingSet(keymap, chord, gesture);
        Assert.Equal("Ctrl+Shift+F9", chord.LSKeymapChordPending);

        TInterface.TKeymapChordCommit(keymap, chord);
        Assert.False(chord.LSKeymapChordActive);
        Assert.Equal("Ctrl+Shift+F9", chord.LSKeymapChordGesture);
        Assert.Equal(string.Empty, chord.LSKeymapChordPending);
        Assert.Equal(3, notices);
    }

    [Fact]
    public void Cancel_DropsPending_KeepsGesture()
    {
        LSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        LSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        string before = chord.LSKeymapChordGesture;

        TInterface.TKeymapChordStart(keymap, chord);
        TInterface.TKeymapPendingSet(keymap, chord, "Alt+K");
        TInterface.TKeymapChordCancel(keymap, chord);

        Assert.Equal(before, chord.LSKeymapChordGesture);
        Assert.False(chord.LSKeymapChordActive);
    }

    [Fact]
    public void Commit_ClearsConflictingChord()
    {
        LSKeymap keymap = TInterface.TKeymapCreate(null);
        IReadOnlyList<LBindingCommand> catalog = TInterface.TBindingCatalogRead();
        LSKeymapChord first = TInterface.TKeymapChordRead(keymap, catalog[0].LBindingCommandToken);
        LSKeymapChord second = TInterface.TKeymapChordRead(keymap, catalog[1].LBindingCommandToken);

        TInterface.TKeymapChordStart(keymap, first);
        TInterface.TKeymapPendingSet(keymap, first, "Ctrl+Alt+Q");
        TInterface.TKeymapChordCommit(keymap, first);
        TInterface.TKeymapChordStart(keymap, second);
        TInterface.TKeymapPendingSet(keymap, second, "ctrl+alt+q");
        TInterface.TKeymapChordCommit(keymap, second);

        Assert.Equal(string.Empty, first.LSKeymapChordGesture);
        Assert.Equal("ctrl+alt+q", second.LSKeymapChordGesture);
    }

    [Fact]
    public void Default_RestoresCatalogGestures_RecordsMirrorChords()
    {
        LSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        LSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        TInterface.TKeymapChordStart(keymap, chord);
        TInterface.TKeymapPendingSet(keymap, chord, "Ctrl+Alt+Z");
        TInterface.TKeymapChordCommit(keymap, chord);

        TInterface.TKeymapDefaultApply(keymap);

        Assert.Equal(TInterface.TBindingDefaultRead(token), chord.LSKeymapChordGesture);
        List<LBindingRecord> records = TInterface.TKeymapRecordsRead(keymap);
        Assert.Equal(keymap.LSKeymapChords.Count, records.Count);
        Assert.Contains(records, record => record.LBindingRecordToken == token
            && record.LBindingRecordGesture == chord.LSKeymapChordGesture);
    }
}
