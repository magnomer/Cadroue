using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditRatchet
{
    private const string TAuditRatchetAudit = "AUDITRATCHET";

    private const string TAuditSettingSuffix = "Setting.cs";

    private const string TAuditLedgerSuffix = "Ledger.json";

    private static readonly string TAuditSettingFolder =
        $"tests/{TAuditNameSetting.TAuditProject}.Tests.Convention/";

    private static readonly string TAuditConventionPath = TAuditSettingFolder + nameof(TAuditConvention) + ".cs";

    private static readonly Regex TAuditGenerationPattern = new(@"TAuditGeneration = (\d+);", RegexOptions.Compiled);

    private static readonly string[] TAuditCeilingSuffixes = ["Ceiling", "Limit", "Ledger"];

    private static readonly string[] TAuditShrinkSuffixes = ["Waiver", "Exempt"];

    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    [Fact]
    public void AuditRatchet_Settings_NeverLoosen()
    {
        string? committed = TAuditCommittedRead(TAuditConventionPath);
        Assert.True(committed is not null, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit, $"{TAuditConventionPath} cannot be read at HEAD, so no setting can be held."));
        Match generation = TAuditGenerationPattern.Match(committed);
        if (!generation.Success
            || int.Parse(generation.Groups[1].Value, CultureInfo.InvariantCulture) != TAuditConvention.TAuditGeneration)
        {
            return;
        }

        IReadOnlyList<string> heads = TAuditHeadRead();
        IReadOnlyList<string> tree = TAuditTreeRead();
        List<string> loosened = [];
        foreach (string name in heads.Except(tree, StringComparer.Ordinal))
        {
            loosened.Add($"  {name} is committed but gone from the working tree");
        }

        foreach (string name in tree.Except(heads, StringComparer.Ordinal))
        {
            loosened.Add($"  {name} is not committed");
        }

        List<string> shared = tree.Intersect(heads, StringComparer.Ordinal).ToList();
        Dictionary<string, Dictionary<string, List<string>>?> before = TAuditValueRead(
            shared.ToDictionary(name => name, name => TAuditCommittedRead(TAuditSettingFolder + name)));
        Dictionary<string, Dictionary<string, List<string>>?> after = TAuditValueRead(
            shared.ToDictionary(name => name, name => (string?)TAuditWorkingRead(name)));
        foreach (string key in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            loosened.AddRange(TAuditLoosenRead(
                key, before.GetValueOrDefault(key), after.GetValueOrDefault(key), before.ContainsKey(key)));
        }

        Assert.True(loosened.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit,
            $"{loosened.Count} setting change(s) loosen a gate without a commit or a generation bump.\n"
            + string.Join('\n', loosened)));
    }

    [Fact]
    public void AuditRatchet_SettingParse_MatchesRuntime()
    {
        Dictionary<string, Dictionary<string, List<string>>?> parsed = TAuditValueRead(TAuditTreeRead()
            .Where(name => name.EndsWith(TAuditSettingSuffix, StringComparison.Ordinal))
            .ToDictionary(name => name, name => (string?)TAuditWorkingRead(name)));
        List<string> drift = [];
        foreach ((string key, Dictionary<string, List<string>>? value) in parsed.OrderBy(
                     pair => pair.Key, StringComparer.Ordinal))
        {
            string[] parts = key.Split('.');
            FieldInfo? field = typeof(TAuditRatchet).Assembly
                .GetType($"{typeof(TAuditRatchet).Namespace}.{parts[0]}")
                ?.GetField(parts[1], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Dictionary<string, List<string>>? runtime = field is null ? null : TAuditRuntimeRead(field.GetValue(null));
            if (value is null)
            {
                drift.Add($"  {key} holds an entry the ratchet cannot read");
            }
            else if (runtime is null || TAuditFixedRead(value, runtime).Any())
            {
                drift.Add($"  {key} reads differently from its runtime value");
            }
        }

        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit,
            $"{drift.Count} setting(s) the ratchet would misread, so a loosening could pass unseen.\n"
            + string.Join('\n', drift)));
    }

    private static IEnumerable<string> TAuditLoosenRead(
        string key,
        Dictionary<string, List<string>>? before,
        Dictionary<string, List<string>>? after,
        bool held)
    {
        if (!held)
        {
            return [$"  {key} is a new setting"];
        }

        if (after is null)
        {
            return [$"  {key} is gone or unreadable"];
        }

        if (before is null)
        {
            return [$"  {key} is unreadable at HEAD"];
        }

        string name = key[(key.IndexOf('.') + 1)..];
        IEnumerable<string> found = TAuditCeilingSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal))
            ? TAuditCeilingRead(before, after, 1)
            : name.EndsWith("Floor", StringComparison.Ordinal) ? TAuditCeilingRead(before, after, -1)
            : TAuditShrinkSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal))
                ? TAuditShrinkRead(before, after)
            : name.EndsWith("Enforced", StringComparison.Ordinal) ? TAuditEnforcedRead(before, after)
            : TAuditFixedRead(before, after);
        return found.Select(entry => $"  {key}{entry}");
    }

    private static IEnumerable<string> TAuditCeilingRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after, int loose)
    {
        foreach (string slot in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            decimal was = TAuditNumberRead(before.GetValueOrDefault(slot));
            decimal now = TAuditNumberRead(after.GetValueOrDefault(slot));
            if (Math.Sign(now - was) == loose)
            {
                yield return $"[{slot}] committed {TAuditNumberFormat(before, slot)}, "
                             + $"now {TAuditNumberFormat(after, slot)}";
            }
        }
    }

    private static IEnumerable<string> TAuditShrinkRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        foreach ((string slot, List<string> items) in after.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            List<string> known = before.GetValueOrDefault(slot) ?? [];
            foreach (string item in items.Where(item => !known.Contains(item, StringComparer.Ordinal)))
            {
                yield return $"[{slot}] gained {item}";
            }
        }
    }

    private static IEnumerable<string> TAuditEnforcedRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        string was = string.Join(',', before.GetValueOrDefault(string.Empty) ?? []);
        string now = string.Join(',', after.GetValueOrDefault(string.Empty) ?? []);
        if (was == bool.TrueString && now != bool.TrueString)
        {
            yield return " switched enforcement off";
        }
    }

    private static IEnumerable<string> TAuditFixedRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        foreach (string slot in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            string[] was = [.. (before.GetValueOrDefault(slot) ?? []).Order(StringComparer.Ordinal)];
            string[] now = [.. (after.GetValueOrDefault(slot) ?? []).Order(StringComparer.Ordinal)];
            bool kept = before.ContainsKey(slot) && after.ContainsKey(slot);
            if (!kept || !was.SequenceEqual(now, StringComparer.Ordinal))
            {
                yield return $"[{slot}] changed from [{string.Join(", ", was)}] to [{string.Join(", ", now)}]";
            }
        }
    }

    private static decimal TAuditNumberRead(List<string>? items)
    {
        return items is [string single] && decimal.TryParse(
            single, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : 0;
    }

    private static string TAuditNumberFormat(Dictionary<string, List<string>> values, string slot)
    {
        return values.TryGetValue(slot, out List<string>? items) ? string.Join(',', items) : "absent";
    }

    private static Dictionary<string, Dictionary<string, List<string>>?> TAuditValueRead(
        IReadOnlyDictionary<string, string?> texts)
    {
        Dictionary<string, Dictionary<string, List<string>>?> values = new(StringComparer.Ordinal);
        List<SyntaxTree> trees = [];
        foreach ((string name, string? text) in texts)
        {
            if (text is null)
            {
                values[name] = null;
            }
            else if (name.EndsWith(TAuditLedgerSuffix, StringComparison.Ordinal))
            {
                values[Path.GetFileNameWithoutExtension(name) + "." + Path.GetFileNameWithoutExtension(name)] =
                    TAuditLedgerParse(text);
            }
            else
            {
                trees.Add(CSharpSyntaxTree.ParseText(text, TAuditSyntaxOptions, name));
            }
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AuditRatchet",
            trees,
            TAuditReferenceRead(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                foreach (FieldDeclarationSyntax field in type.Members.OfType<FieldDeclarationSyntax>().Where(field =>
                             field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)
                                                             || modifier.IsKind(SyntaxKind.ConstKeyword))))
                {
                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                    {
                        ExpressionSyntax? held = variable.Initializer?.Value;
                        values[$"{type.Identifier.ValueText}.{variable.Identifier.ValueText}"] =
                            held is null ? null : TAuditExpressionRead(model, held);
                    }
                }
            }
        }

        return values;
    }

    private static Dictionary<string, List<string>>? TAuditExpressionRead(SemanticModel model, ExpressionSyntax value)
    {
        Optional<object?> constant = model.GetConstantValue(value);
        if (constant.HasValue)
        {
            return new Dictionary<string, List<string>>(StringComparer.Ordinal)
            {
                [string.Empty] = [TAuditScalarFormat(constant.Value)],
            };
        }

        List<string>? items = TAuditListRead(model, value);
        if (items is not null)
        {
            return new Dictionary<string, List<string>>(StringComparer.Ordinal) { [string.Empty] = items };
        }

        if (value is not BaseObjectCreationExpressionSyntax creation)
        {
            return null;
        }

        Dictionary<string, List<string>> map = new(StringComparer.Ordinal);
        foreach (ExpressionSyntax entry in creation.Initializer?.Expressions ?? [])
        {
            (ExpressionSyntax? slot, ExpressionSyntax? held) = entry switch
            {
                AssignmentExpressionSyntax
                {
                    Left: ImplicitElementAccessSyntax { ArgumentList.Arguments: [ArgumentSyntax only] }
                } assignment => (only.Expression, assignment.Right),
                InitializerExpressionSyntax { Expressions: [ExpressionSyntax first, ExpressionSyntax second] }
                    => (first, second),
                _ => ((ExpressionSyntax?)null, (ExpressionSyntax?)null),
            };
            Optional<object?> key = slot is null ? default : model.GetConstantValue(slot);
            Optional<object?> scalar = held is null ? default : model.GetConstantValue(held);
            List<string>? list = held is null ? null : TAuditListRead(model, held);
            if (!key.HasValue || (!scalar.HasValue && list is null))
            {
                return null;
            }

            map[TAuditScalarFormat(key.Value)] = scalar.HasValue ? [TAuditScalarFormat(scalar.Value)] : list!;
        }

        return map;
    }

    private static List<string>? TAuditListRead(SemanticModel model, ExpressionSyntax value)
    {
        IEnumerable<ExpressionSyntax?>? elements = value switch
        {
            CollectionExpressionSyntax collection => collection.Elements
                .Select(element => (element as ExpressionElementSyntax)?.Expression),
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
            ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
            _ => null,
        };
        if (elements is null)
        {
            return null;
        }

        List<string> items = [];
        foreach (ExpressionSyntax? element in elements)
        {
            Optional<object?> constant = element is null ? default : model.GetConstantValue(element);
            if (!constant.HasValue)
            {
                return null;
            }

            items.Add(TAuditScalarFormat(constant.Value));
        }

        return items;
    }

    private static Dictionary<string, List<string>>? TAuditLedgerParse(string text)
    {
        Dictionary<string, List<string>> map = new(StringComparer.Ordinal);
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            foreach (JsonProperty kind in document.RootElement.EnumerateObject())
            {
                foreach (JsonProperty path in kind.Value.EnumerateObject())
                {
                    map[$"{kind.Name} {path.Name}"] = [path.Value.GetInt32().ToString(CultureInfo.InvariantCulture)];
                }
            }
        }
        catch (Exception failure) when (failure is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }

        return map;
    }

    private static Dictionary<string, List<string>>? TAuditRuntimeRead(object? value)
    {
        Dictionary<string, List<string>> map = new(StringComparer.Ordinal);
        switch (value)
        {
            case null:
                return null;
            case string or bool or int or double or long or char:
                map[string.Empty] = [TAuditScalarFormat(value)];
                return map;
            case IDictionary pairs:
                foreach (DictionaryEntry pair in pairs)
                {
                    map[TAuditScalarFormat(pair.Key)] = pair.Value is string[] list
                        ? [.. list]
                        : [TAuditScalarFormat(pair.Value)];
                }

                return map;
            case IEnumerable<string> list:
                map[string.Empty] = [.. list];
                return map;
            default:
                return null;
        }
    }

    private static string TAuditScalarFormat(object? value)
    {
        return value switch
        {
            null => "null",
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
        };
    }

    private static List<MetadataReference> TAuditReferenceRead()
    {
        string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        return trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal)
                           || Path.GetFileName(path).Equals("netstandard.dll", StringComparison.Ordinal)
                           || Path.GetFileName(path).Equals("mscorlib.dll", StringComparison.Ordinal))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static IReadOnlyList<string> TAuditTreeRead()
    {
        string folder = Path.Combine(TAuditSource.TAuditRootRead(), TAuditSettingFolder);
        return Directory.EnumerateFiles(folder)
            .Select(path => Path.GetFileName(path))
            .Where(TAuditHeldCheck)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> TAuditHeadRead()
    {
        string? listing = TAuditGitRead("ls-tree", "--name-only", "HEAD", TAuditSettingFolder);
        Assert.True(listing is not null, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit, $"git cannot list {TAuditSettingFolder} at HEAD, so no setting can be held."));
        return listing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path[(path.LastIndexOf('/') + 1)..])
            .Where(TAuditHeldCheck)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static bool TAuditHeldCheck(string name)
    {
        return name.StartsWith("TAudit", StringComparison.Ordinal)
               && (name.EndsWith(TAuditSettingSuffix, StringComparison.Ordinal)
                   || name.EndsWith(TAuditLedgerSuffix, StringComparison.Ordinal));
    }

    private static string TAuditWorkingRead(string name)
    {
        return File.ReadAllText(Path.Combine(TAuditSource.TAuditRootRead(), TAuditSettingFolder, name));
    }

    private static string? TAuditCommittedRead(string path)
    {
        return TAuditGitRead("show", $"HEAD:{path}");
    }

    private static string? TAuditGitRead(params string[] arguments)
    {
        ProcessStartInfo info = new("git")
        {
            WorkingDirectory = TAuditSource.TAuditRootRead(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using Process? process = Process.Start(info);
        if (process is null)
        {
            return null;
        }

        string output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : null;
    }
}
