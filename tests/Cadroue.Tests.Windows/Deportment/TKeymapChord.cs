using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TKeymapChord
{
    [Fact]
    public void Capture_PendingBecomesGesture_OnCommit()
    {
        QSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        QSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        int notices = 0;
        TInterface.TKeymapAttach(keymap, _ => notices++);
        string gesture = TInterface.TBindingGestureFormat("F9", true, false, true, false);

        TInterface.TKeymapPendingSet(keymap, chord, gesture);
        Assert.Equal(string.Empty, chord.QSKeymapChordPending);

        TInterface.TKeymapChordStart(keymap, chord);
        Assert.True(chord.QSKeymapChordActive);
        TInterface.TKeymapPendingSet(keymap, chord, gesture);
        Assert.Equal("Ctrl+Shift+F9", chord.QSKeymapChordPending);

        TInterface.TKeymapChordCommit(keymap, chord);
        Assert.False(chord.QSKeymapChordActive);
        Assert.Equal("Ctrl+Shift+F9", chord.QSKeymapChordGesture);
        Assert.Equal(string.Empty, chord.QSKeymapChordPending);
        Assert.Equal(3, notices);
    }

    [Fact]
    public void Cancel_DropsPending_KeepsGesture()
    {
        QSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        QSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        string before = chord.QSKeymapChordGesture;

        TInterface.TKeymapChordStart(keymap, chord);
        TInterface.TKeymapPendingSet(keymap, chord, "Alt+K");
        TInterface.TKeymapChordCancel(keymap, chord);

        Assert.Equal(before, chord.QSKeymapChordGesture);
        Assert.False(chord.QSKeymapChordActive);
    }

    [Fact]
    public void Commit_ClearsConflictingChord()
    {
        QSKeymap keymap = TInterface.TKeymapCreate(null);
        IReadOnlyList<LBindingCommand> catalog = TInterface.TBindingCatalogRead();
        QSKeymapChord first = TInterface.TKeymapChordRead(keymap, catalog[0].LBindingCommandToken);
        QSKeymapChord second = TInterface.TKeymapChordRead(keymap, catalog[1].LBindingCommandToken);

        TInterface.TKeymapChordStart(keymap, first);
        TInterface.TKeymapPendingSet(keymap, first, "Ctrl+Alt+Q");
        TInterface.TKeymapChordCommit(keymap, first);
        TInterface.TKeymapChordStart(keymap, second);
        TInterface.TKeymapPendingSet(keymap, second, "ctrl+alt+q");
        TInterface.TKeymapChordCommit(keymap, second);

        Assert.Equal(string.Empty, first.QSKeymapChordGesture);
        Assert.Equal("ctrl+alt+q", second.QSKeymapChordGesture);
    }

    [Fact]
    public void Default_RestoresCatalogGestures_RecordsMirrorChords()
    {
        QSKeymap keymap = TInterface.TKeymapCreate(null);
        string token = TInterface.TBindingCatalogRead()[0].LBindingCommandToken;
        QSKeymapChord chord = TInterface.TKeymapChordRead(keymap, token);
        TInterface.TKeymapChordStart(keymap, chord);
        TInterface.TKeymapPendingSet(keymap, chord, "Ctrl+Alt+Z");
        TInterface.TKeymapChordCommit(keymap, chord);

        TInterface.TKeymapDefaultApply(keymap);

        Assert.Equal(TInterface.TBindingDefaultRead(token), chord.QSKeymapChordGesture);
        List<LBindingEntry> records = TInterface.TKeymapRecordsRead(keymap);
        Assert.Equal(keymap.QSKeymapChords.Count, records.Count);
        Assert.Contains(records, record => record.LBindingRecordToken == token
            && record.LBindingRecordGesture == chord.QSKeymapChordGesture);
    }
}
