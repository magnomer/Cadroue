using System.Collections;
using System.Globalization;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditRatchetValue
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    public static Dictionary<string, Dictionary<string, List<string>>?> TAuditValueRead(
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
            else if (name.EndsWith(TAuditRatchetFile.TAuditLedgerSuffix, StringComparison.Ordinal))
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

    public static Dictionary<string, List<string>>? TAuditRuntimeRead(object? value)
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
}
