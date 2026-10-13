using System.Collections;
using System.Reflection;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Interface;

internal sealed class TSceneValue
{
    internal TSceneValue(LSceneDocument scene)
    {
        TSceneData = scene;
    }

    internal LSceneDocument TSceneData { get; }
}

internal sealed class TScene : IDisposable
{
    private const string TSceneStoreName = "LScenePresets.json";
    private readonly string tSceneRoot;
    private readonly string? _tScenePrevious;

    internal TScene()
    {
        _tScenePrevious = LScene.LSceneRootRead();
        tSceneRoot = Path.Combine(Path.GetTempPath(), "cadroue-scene-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tSceneRoot);
        LScene.LSceneRootSet(tSceneRoot);
    }

    internal TSceneValue TSceneRecordCreate(string name, int marker = 1) =>
        new(TSceneSample.TSceneCreate(name, marker));

    internal void TSceneSave(TSceneValue scene) => LScene.LSceneSave(scene.TSceneData);

    internal TSceneValue? TSceneRead(string name)
    {
        LSceneDocument? scene = LScene.LSceneRead(name);
        return scene is null ? null : new TSceneValue(scene);
    }

    internal bool TSceneDelete(string name) => LScene.LSceneDelete(name);

    internal IReadOnlyList<string> TSceneNames => LScene.LSceneNames;

    internal void TSceneDiskLoad() => LScene.LSceneRootSet(tSceneRoot);

    internal bool TScenePersistCheck(TSceneValue left, TSceneValue right) =>
        TSceneValueCheck(left.TSceneData, right.TSceneData);

    internal TSceneValue TSceneExternalMatch(TSceneValue scene)
    {
        string path = Path.Combine(tSceneRoot, "external.cadroue-scene");
        LScene.LSceneFileSave(scene.TSceneData, path);
        return new TSceneValue(
            LScene.LSceneFileLoad(path)
            ?? throw new InvalidDataException("Production returned no scene for a scene file it saved."));
    }

    internal static LSceneDocument TSceneRawCreate() => new();

    internal static LSceneTabEntry TSceneTabCreate() => new();

    internal static LSceneInspectorEntry TSceneInspectorCreate() => new();

    internal static LSidecarEditEntry TSceneEditCreate() => new();

    internal static LSidecarAudioEntry TSceneAudioCreate() => new();

    internal static LSceneDocument TSceneNormalize(LSceneDocument? raw) => LScene.LSceneNormalize(raw);

    internal static int TSceneVersionCurrent => LScene.LSceneVersionCurrent;

    internal static IReadOnlyList<LSceneDocument> TSceneCatalogueNormalize(List<LSceneDocument>? raw) =>
        LScene.LSceneCatalogueNormalize(raw);

    internal void TSceneMalformSave()
    {
        File.WriteAllText(Path.Combine(tSceneRoot, TSceneStoreName), "{ definitely-not-a-scene");
    }

    internal bool TSceneMatch(TSceneValue left, TSceneValue right) =>
        LScene.LSceneMatch(left.TSceneData, right.TSceneData);

    internal void TSceneFieldChange(TSceneValue scene)
    {
        scene.TSceneData.LSceneTabLayouts[0].LSceneAutoRelay =
            !scene.TSceneData.LSceneTabLayouts[0].LSceneAutoRelay;
    }

    internal void TSceneNameChange(TSceneValue scene, string name)
    {
        scene.TSceneData.LSceneName = name;
    }

    internal void TSceneTabChange(TSceneValue scene) => scene.TSceneData.LSceneTabIndex = 0;

    internal void TSceneWidthChange(TSceneValue scene) =>
        scene.TSceneData.LSceneTabLayouts[0].LScenePanelWidths.Add(999);

    internal void TSceneScaleCreate(TSceneValue scene, double scale)
    {
        foreach (LSceneTabEntry layout in scene.TSceneData.LSceneTabLayouts)
        {
            for (int index = 0; index < layout.LScenePanelWidths.Count; index++)
            {
                layout.LScenePanelWidths[index] *= scale;
            }
        }
    }

    internal void TSceneVersionChange(TSceneValue scene) =>
        scene.TSceneData.LSceneVersion = 0;

    internal void TSceneReverseCreate(TSceneValue scene) =>
        scene.TSceneData.LSceneTabNames.Reverse();

    internal void TSceneWidthCreate(TSceneValue scene)
    {
        foreach (LSceneTabEntry layout in scene.TSceneData.LSceneTabLayouts)
        {
            layout.LScenePanelWidths.Reverse();
        }
    }

    internal void TSceneCurrentSave(TSceneValue scene)
    {
        LScene.LSceneStateSave(scene.TSceneData);
        LScene.LSceneCurrentLoad();
    }

    internal string TSceneCurrentName => LScene.LSceneCurrent.LSceneName;

    internal string TSceneActiveName => LScene.LSceneActiveName;

    public void Dispose()
    {
        LScene.LSceneRootSet(_tScenePrevious);
        try
        {
            Directory.Delete(tSceneRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool TSceneValueCheck(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.GetType() != right.GetType())
        {
            return false;
        }

        Type type = left.GetType();
        if (type.IsValueType || left is string)
        {
            return left.Equals(right);
        }

        if (left is IEnumerable leftItems && right is IEnumerable rightItems)
        {
            IEnumerator leftIterator = leftItems.GetEnumerator();
            IEnumerator rightIterator = rightItems.GetEnumerator();
            while (true)
            {
                bool leftNext = leftIterator.MoveNext();
                bool rightNext = rightIterator.MoveNext();
                if (leftNext != rightNext)
                {
                    return false;
                }

                if (!leftNext)
                {
                    return true;
                }

                if (!TSceneValueCheck(leftIterator.Current, rightIterator.Current))
                {
                    return false;
                }
            }
        }

        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .All(property => TSceneValueCheck(property.GetValue(left), property.GetValue(right)));
    }
}
