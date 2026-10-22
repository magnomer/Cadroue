using System;
using System.Collections.Generic;
using System.Reflection;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TOptionsPolicy
{
    [Fact]
    public void Apply_PreservesCompatibilityDraftEditsAfterConstruction()
    {
        var requests = new List<LOptionsRequest>();
        var port = new TOptionsPort(requests);
        LPreferenceState draft = Activator.CreateInstance<LPreferenceState>();
        draft.LPreferenceAutoplay = false;
        draft.LPreferencePreviewEngine = "Flyleaf";
        draft.LPreferenceFfmpegFolder = string.Empty;
        var options = new CSOptions(
            TAtelier.TAtelierCreate().CAtelierUsher,
            draft,
            port.CSOptionsPortRead,
            port.CSOptionsPortApply);
        LPreferenceState? saved = null;
        PropertyInfo saveSeam = typeof(LPreference).GetProperty(nameof(LPreference.LPreferenceSaveSeam))!;
        saveSeam.SetValue(null, (Func<LPreferenceState, bool>)(state =>
        {
            saved = state;
            return false;
        }));
        try
        {
            draft.LPreferenceAutoplay = true;
            draft.LPreferencePreviewEngine = "Mpv";
            draft.LPreferenceFfmpegFolder = "pending-renderer";

            LOptionsApplyResult result = options.CSOptionsApply();

            Assert.False(result.CSOptionsSaved);
            Assert.NotNull(saved);
            Assert.NotSame(draft, saved);
            Assert.True(saved.LPreferenceAutoplay);
            Assert.Equal("Mpv", saved.LPreferencePreviewEngine);
            Assert.Equal("pending-renderer", saved.LPreferenceFfmpegFolder);
            Assert.Equal(LPreviewEngine.LPreviewEngineMpv, requests[^1].LOptionsRequestEngine);
        }
        finally
        {
            saveSeam.SetValue(null, null);
        }
    }

    [Fact]
    public void RecordCheck_PreservesPendingGeneralEdits()
    {
        LPreferenceState draft = Activator.CreateInstance<LPreferenceState>();
        draft.LPreferenceStartupMode = "LastSession";
        draft.LPreferenceVerticalTabs = false;
        var port = new TOptionsPort([]);
        var options = new CSOptions(
            TAtelier.TAtelierCreate().CAtelierUsher,
            draft,
            port.CSOptionsPortRead,
            port.CSOptionsPortApply);
        options.CSOptionsUpdate("DefaultTab");
        options.CSOptionsUpdate(true);

        _ = options.CSOptionsRecordCheck();
        LOptionsSnapshot afterRecord = options.CSOptionsSnapshotRead();
        options.CSOptionsLevelSet(LCustodyFamily.LCustodyFamilyEncode, 1);
        LOptionsSnapshot afterLevel = options.CSOptionsSnapshotRead();

        Assert.Equal("DefaultTab", afterRecord.CSOptionsStartup);
        Assert.True(afterRecord.CSOptionsOrientation);
        Assert.Equal("DefaultTab", afterLevel.CSOptionsStartup);
        Assert.True(afterLevel.CSOptionsOrientation);
        Assert.Equal(1, afterLevel.CSOptionsEncodeLevel);
    }

    [Fact]
    public void Apply_PreservesPersistenceRendererCallbackOrder()
    {
        var requests = new List<LOptionsRequest>();
        var port = new TOptionsPort(requests);
        LPreferenceState draft = Activator.CreateInstance<LPreferenceState>();
        draft.LPreferenceAutoplay = !draft.LPreferenceAutoplay;
        draft.LPreferencePreviewEngine = "Mpv";
        CUsher usher = TAtelier.TAtelierCreate().CAtelierUsher;
        LPreferenceState? saved = null;
        PropertyInfo saveSeam = typeof(LPreference).GetProperty(nameof(LPreference.LPreferenceSaveSeam))!;
        saveSeam.SetValue(null, (Func<LPreferenceState, bool>)(state =>
        {
            saved = state;
            requests.Add(new(LOptionsRequestKind.LOptionsRequestDefault));
            return false;
        }));
        try
        {
            var options = new CSOptions(
                usher,
                draft,
                port.CSOptionsPortRead,
                port.CSOptionsPortApply);

            LOptionsApplyResult result = options.CSOptionsApply();
            requests.Add(new(LOptionsRequestKind.LOptionsRequestLocation));

            Assert.False(result.CSOptionsSaved);
            Assert.NotNull(saved);
            Assert.NotSame(draft, saved);
            Assert.Equal(draft.LPreferenceAutoplay, saved.LPreferenceAutoplay);
            Assert.Equal(
                new[]
                {
                    LOptionsRequestKind.LOptionsRequestDefault,
                    LOptionsRequestKind.LOptionsRequestRenderer,
                    LOptionsRequestKind.LOptionsRequestLocation
                },
                requests.ConvertAll(request => request.LOptionsRequestKind));
            Assert.Equal(LPreviewEngine.LPreviewEngineMpv, requests[1].LOptionsRequestEngine);
        }
        finally
        {
            saveSeam.SetValue(null, null);
        }
    }

    [Fact]
    public void WorkspaceResolve_KeepsCurrentWhenDestinationOccupied()
    {
        var requests = new List<LOptionsRequest>();
        var port = new TOptionsPort(requests);
        var currentState = (LPreferenceState)typeof(LPreference)
            .GetProperty("LPreferenceStateCurrent")!.GetValue(null)!;
        string current = currentState.LPreferenceWorkspaceFolder;
        var options = new CSOptions(
            TAtelier.TAtelierCreate().CAtelierUsher,
            Activator.CreateInstance<LPreferenceState>(),
            port.CSOptionsPortRead,
            port.CSOptionsPortApply);

        string? destination = null;
        string kept = options.CSOptionsWorkspaceResolve("next", (_, occupied) => destination = occupied);

        Assert.Equal(current, kept);
        Assert.Equal("resolved:next", destination);
        Assert.Equal(
            new[]
            {
                LOptionsRequestKind.LOptionsRequestResolution,
                LOptionsRequestKind.LOptionsRequestResolution,
                LOptionsRequestKind.LOptionsRequestOccupancy
            },
            requests.ConvertAll(request => request.LOptionsRequestKind));
    }

    private sealed class TOptionsPort
    {
        private readonly List<LOptionsRequest> tOptionsPortRequests;

        internal TOptionsPort(List<LOptionsRequest> requests) => tOptionsPortRequests = requests;

        public LOptionsFact CSOptionsPortRead(LOptionsRequest request)
        {
            tOptionsPortRequests.Add(request);
            return request.LOptionsRequestKind switch
            {
                LOptionsRequestKind.LOptionsRequestResolution =>
                    new("resolved:" + request.LOptionsRequestPath, 0, 0, false),
                LOptionsRequestKind.LOptionsRequestOccupancy =>
                    new(string.Empty, 0, 0, true),
                _ => new(string.Empty, 0, 0, false)
            };
        }

        public LOptionsFact CSOptionsPortApply(LOptionsRequest request)
        {
            tOptionsPortRequests.Add(request);
            return new(string.Empty, 0, 0, false);
        }
    }
}
