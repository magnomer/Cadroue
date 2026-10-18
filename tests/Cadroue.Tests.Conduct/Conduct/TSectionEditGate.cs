using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionEditGate
{
    private readonly TFlowPort tSectionEditPort = new();
    private readonly CSection tSectionEditOwner;
    private readonly CSectionEdit tSectionEditGate;
    private int tSectionEditTells;

    public TSectionEditGate()
    {
        tSectionEditOwner = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tSectionEditPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow
            .CFlowSection;
        tSectionEditGate = tSectionEditOwner.CSectionList.CSectionEdit;
        TSectionEditRead().TSectionCount = 2;
        TSectionEditRead().TSectionRows =
        [
            ("", "p", "", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), 2, false),
            ("b", "", "", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(4), 0, true)
        ];
        tSectionEditOwner.CSectionList.CSectionRowsSync();
        tSectionEditOwner.CSectionList.CSectionRowsChanged += () => tSectionEditTells++;
    }

    private TSectionPort TSectionEditRead() => tSectionEditPort.TFlowSection;

    [Fact]
    public void Start_TakesTheHeldRowsText_SerialGrows()
    {
        int serial = tSectionEditGate.CSectionEditSerial;

        tSectionEditOwner.CSectionList.CSectionRenameHandle(1, 2);
        Assert.Equal(serial + 1, tSectionEditGate.CSectionEditSerial);
        tSectionEditGate.CSectionEditCommit();
        tSectionEditOwner.CSectionList.CSectionRenameHandle(0, 2);

        Assert.Equal(serial + 2, tSectionEditGate.CSectionEditSerial);
        Assert.Equal(0, tSectionEditGate.CSectionEditIndex);
        Assert.Equal(["select:1", "name:1:b::", "select:0"], TSectionEditRead().TSectionCalls);
    }

    [Fact]
    public void Handle_OnlyTheLiveEditorWithFocusOutside_CommitsTrimmed()
    {
        tSectionEditOwner.CSectionList.CSectionRenameHandle(0, 2);
        int serial = tSectionEditGate.CSectionEditSerial;
        tSectionEditGate.CSectionEditSet(" a ", "p ", " s");

        tSectionEditGate.CSectionEditHandle(serial - 1, false);
        tSectionEditGate.CSectionEditHandle(serial, true);
        Assert.Equal(0, tSectionEditGate.CSectionEditIndex);
        tSectionEditGate.CSectionEditHandle(serial, false);

        Assert.Null(tSectionEditGate.CSectionEditIndex);
        Assert.False(tSectionEditOwner.CSectionList.CSectionRowsRead()[0].CSectionRowEditing);
        Assert.Equal(["select:0", "name:0:a:p:s"], TSectionEditRead().TSectionCalls);
        Assert.Equal(2, tSectionEditTells);
    }

    [Fact]
    public void Commit_WithoutAnEdit_RunsNothing()
    {
        tSectionEditGate.CSectionEditCommit();

        Assert.Empty(TSectionEditRead().TSectionCalls);
        Assert.Equal(0, tSectionEditTells);
    }

    [Fact]
    public void Commit_WhileNotEditable_NamesNothing_ClosesTheEditor()
    {
        tSectionEditOwner.CSectionList.CSectionRenameHandle(1, 2);
        tSectionEditGate.CSectionEditSet("c", "", "");
        tSectionEditOwner.CSectionEditableSet(false);

        tSectionEditGate.CSectionEditCommit();

        Assert.Null(tSectionEditGate.CSectionEditIndex);
        Assert.Equal(["select:1"], TSectionEditRead().TSectionCalls);
        Assert.Equal(2, tSectionEditTells);
    }

    [Fact]
    public void Cancel_DropsTheTextWithoutNaming_TellsRebuild()
    {
        tSectionEditOwner.CSectionList.CSectionRenameHandle(0, 2);
        tSectionEditGate.CSectionEditSet("c", "", "");

        tSectionEditGate.CSectionEditCancel();
        tSectionEditGate.CSectionEditCommit();

        Assert.Null(tSectionEditGate.CSectionEditIndex);
        Assert.Equal(["select:0"], TSectionEditRead().TSectionCalls);
        Assert.Equal(2, tSectionEditTells);
    }
}
