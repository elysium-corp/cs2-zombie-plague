using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EventDocsGenerator;

internal static partial class Program
{
    private const string CatalogKind = "elysium.event-catalog";
    private const string PackageKind = "elysium.documentation.catalog";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly SourceDefinition[] Sources =
    [
        new("zombie-plague", "ZombiePlague.Api", "Players", true, "Events.Players.", "ZombiePlague.Api/Events/IZombiePlaguePlayerEvents.cs"),
        new("zombie-plague", "ZombiePlague.Api", "Classes", true, "Events.Classes.", "ZombiePlague.Api/Events/IZombiePlagueClassEvents.cs"),
        new("zombie-plague", "ZombiePlague.Api", "Rounds", true, "Events.Rounds.", "ZombiePlague.Api/Events/IZombiePlagueRoundEvents.cs"),
        new("zombie-plague", "ZombiePlague.Api", "Combat", true, "Events.Combat.", "ZombiePlague.Api/Events/IZombiePlagueCombatEvents.cs"),
        new("custom-equipment", "CustomEquipment.Api", "Items", true, "Events.Items.", "CustomEquipment.Api/Events/ICustomEquipmentItemEvents.cs"),
        new("custom-equipment", "CustomEquipment.Api", "Weapons", true, "Events.Weapons.", "CustomEquipment.Api/Events/ICustomEquipmentWeaponEvents.cs"),
        new("custom-equipment", "CustomEquipment.Api", "Grenades", true, "Events.Grenades.", "CustomEquipment.Api/Events/ICustomEquipmentGrenadeEvents.cs"),
        new("custom-equipment", "CustomEquipment.Api", "Mines", true, "Events.Mines.", "CustomEquipment.Api/Events/ICustomEquipmentMineEvents.cs"),
        new("supply-box", "SupplyBox.Api", "Supply boxes", false, "Events.", "SupplyBox.Api/Events/ISupplyBoxEvents.cs"),
        new("economy", "Economy.Api", "Transactions", false, "Events.Transactions.", "Economy.Api/Events/IEconomyTransactionEvents.cs"),
        new("economy", "Economy.Api", "Accounts", true, "Events.Accounts.", "Economy.Api/Events/IEconomyAccountEvents.cs"),
    ];

    private static readonly HashSet<string> Frequencies =
        ["Редко", "Раунд", "Игрок", "Часто", "Горячий путь"];

    private static readonly HashSet<string> Loads = ["Низкая", "Средняя", "Высокая"];
    private static readonly HashSet<string> RiskLevels = ["Низкий", "Средний", "Высокий", "Критический"];
    private static readonly HashSet<string> Threads = ["Игровой поток", "Фоновая очередь БД"];

    public static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            var root = FindRepositoryRoot();
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "generate";
            var options = ParseOptions(args.Skip(1));
            var catalog = BuildCatalog(root);
            var markdown = BuildMarkdown(root, catalog.Events);
            var catalogJson = Serialize(catalog);

            return command switch
            {
                "generate" => Generate(root, markdown, catalogJson),
                "check" => Check(root, markdown, catalogJson),
                "package" => WritePackage(root, catalog, options),
                _ => throw new DocumentationException(
                    $"Unknown command '{command}'. Use generate, check or package."),
            };
        }
        catch (DocumentationException exception)
        {
            Console.Error.WriteLine($"Event documentation error: {exception.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 2;
        }
    }

    private static CatalogDocument BuildCatalog(string root)
    {
        var events = new List<EventDocumentation>();
        var contexts = DiscoverContexts(root);

        foreach (var source in Sources)
        {
            var path = Path.Combine(root, source.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new DocumentationException($"Event source does not exist: {source.RelativePath}");
            }

            events.AddRange(ParseSource(source, File.ReadAllText(path), contexts));
        }

        var duplicate = events.GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new DocumentationException($"Duplicate event id: {duplicate.Key}");
        }

        var discoveredCount = DiscoverSubscriptionCount(root);
        if (events.Count != discoveredCount)
        {
            throw new DocumentationException(
                $"Catalog covers {events.Count} events, but {discoveredCount} IHookSubscription properties were found in *.Api/Events. Update the source manifest.");
        }

        var projects = Sources.Select(source => new { source.ProjectKey, source.Project })
            .Distinct()
            .Select(project =>
            {
                var projectEvents = events.Where(item => item.ProjectKey == project.ProjectKey).ToArray();
                var groups = projectEvents.GroupBy(item => item.Group, StringComparer.Ordinal)
                    .Select(group => new CatalogGroup(group.Key, group.Count()))
                    .ToArray();
                return new CatalogProject(project.ProjectKey, project.Project, projectEvents.Length, groups);
            })
            .ToArray();

        return new CatalogDocument(2, CatalogKind, events.Count, projects, events);
    }

    private static IEnumerable<EventDocumentation> ParseSource(SourceDefinition source, string text,
        IReadOnlyDictionary<string, ContextDocumentation> contexts)
    {
        var matches = EventPropertyRegex().Matches(text);
        if (matches.Count == 0)
        {
            throw new DocumentationException($"No documented events found in {source.RelativePath}");
        }

        foreach (Match match in matches)
        {
            var documentation = ParseXmlDocumentation(match.Groups["docs"].Value, source.RelativePath);
            var eventName = match.Groups["name"].Value;
            var context = match.Groups["context"].Value;
            if (!contexts.TryGetValue(context, out var contextDocumentation))
                throw new DocumentationException($"Context '{context}' used by {eventName} was not found or is ambiguous.");
            var apiPath = source.PublicPrefix + eventName;
            var metadata = ReadMetadata(documentation, source.RelativePath, apiPath);
            ValidateMetadata(metadata, source.RelativePath, apiPath);

            var propertyIndex = match.Groups["property"].Index;
            var line = 1 + text[..propertyIndex].Count(character => character == '\n');

            yield return new EventDocumentation(
                $"{source.Project}:{apiPath}",
                source.ProjectKey,
                source.Project,
                source.Group,
                apiPath,
                eventName,
                context,
                contextDocumentation.IsCancellable,
                contextDocumentation.Parameters,
                XmlText(documentation.Element("summary")),
                metadata["Когда"],
                metadata["Частота"],
                metadata["Нагрузка"],
                RiskLevel(metadata["Риск"]),
                metadata["Риск"],
                metadata["Поток"],
                source.RelativePath,
                line);
        }
    }

    private static IReadOnlyDictionary<string, ContextDocumentation> DiscoverContexts(string root)
    {
        var result = new Dictionary<string, ContextDocumentation>(StringComparer.Ordinal);
        foreach (var apiDirectory in Directory.EnumerateDirectories(root, "*.Api", SearchOption.TopDirectoryOnly))
        foreach (var file in Directory.EnumerateFiles(apiDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match type in ContextTypeRegex().Matches(text))
            {
                var name = type.Groups["name"].Value;
                var open = type.Index + type.Length - 1;
                if (open < 0) continue;
                var depth = 1; var end = open + 1;
                while (end < text.Length && depth > 0) { if (text[end] == '{') depth++; else if (text[end] == '}') depth--; end++; }
                if (depth != 0) throw new DocumentationException($"Unbalanced context declaration in {file}.");
                var body = text[(open + 1)..(end - 1)];
                var parameters = ContextPropertyRegex().Matches(body).Select(property =>
                {
                    var propertyType = property.Groups["type"].Value.Trim();
                    var accessor = property.Groups["accessor"].Value;
                    return new ContextParameter(property.Groups["name"].Value, propertyType,
                        propertyType.EndsWith("?", StringComparison.Ordinal), accessor.Contains("set;", StringComparison.Ordinal));
                }).Where(parameter => parameter.Name != "IsCancelled").ToArray();
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                var context = new ContextDocumentation(type.Groups["interfaces"].Value.Contains("IPreHookContext", StringComparison.Ordinal), parameters, relative);
                if (!result.TryAdd(name, context)) result.Remove(name);
            }
        }
        return result;
    }

    private static XElement ParseXmlDocumentation(string raw, string sourcePath)
    {
        var lines = raw.Split('\n')
            .Select(line => XmlCommentPrefixRegex().Replace(line, string.Empty))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        try
        {
            return XElement.Parse("<member>" + string.Join('\n', lines) + "</member>");
        }
        catch (Exception exception)
        {
            throw new DocumentationException($"Invalid XML comments in {sourcePath}: {exception.Message}");
        }
    }

    private static Dictionary<string, string> ReadMetadata(
        XElement documentation,
        string sourcePath,
        string apiPath)
    {
        if (string.IsNullOrWhiteSpace(XmlText(documentation.Element("summary"))))
        {
            throw new DocumentationException($"{sourcePath}: {apiPath} has no summary.");
        }

        var result = documentation.Element("remarks")?
            .Descendants("item")
            .Select(item => new
            {
                Term = XmlText(item.Element("term")),
                Description = XmlText(item.Element("description")),
            })
            .Where(item => item.Term.Length > 0)
            .ToDictionary(item => item.Term, item => item.Description, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var required in new[] { "Когда", "Частота", "Нагрузка", "Риск", "Поток" })
        {
            if (!result.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new DocumentationException($"{sourcePath}: {apiPath} has no '{required}' metadata.");
            }
        }

        return result;
    }

    private static void ValidateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string sourcePath,
        string apiPath)
    {
        ValidateValue(Frequencies, metadata["Частота"], "Частота", sourcePath, apiPath);
        ValidateValue(Loads, metadata["Нагрузка"], "Нагрузка", sourcePath, apiPath);
        ValidateValue(RiskLevels, RiskLevel(metadata["Риск"]), "Риск", sourcePath, apiPath);
        ValidateValue(Threads, metadata["Поток"], "Поток", sourcePath, apiPath);
    }

    private static void ValidateValue(
        IReadOnlySet<string> allowed,
        string value,
        string field,
        string sourcePath,
        string apiPath)
    {
        if (!allowed.Contains(value))
        {
            throw new DocumentationException(
                $"{sourcePath}: {apiPath} has invalid {field} '{value}'. Allowed: {string.Join(", ", allowed)}.");
        }
    }

    private static int DiscoverSubscriptionCount(string root)
    {
        var count = 0;
        foreach (var apiDirectory in Directory.EnumerateDirectories(root, "*.Api", SearchOption.TopDirectoryOnly))
        {
            var eventsDirectory = Path.Combine(apiDirectory, "Events");
            if (!Directory.Exists(eventsDirectory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(eventsDirectory, "*.cs", SearchOption.AllDirectories))
            {
                count += SubscriptionRegex().Count(File.ReadAllText(file));
            }
        }

        return count;
    }

    private static string BuildMarkdown(string root, IReadOnlyList<EventDocumentation> events)
    {
        var intro = ReadTemplate(root, "docs/events.intro.md");
        var footer = ReadTemplate(root, "docs/events.footer.md");
        var builder = new StringBuilder();
        builder.AppendLine(intro.TrimEnd()).AppendLine();

        foreach (var project in Sources.Select(source => new { source.ProjectKey, source.Project }).Distinct())
        {
            builder.Append("## ").AppendLine(project.Project).AppendLine();
            var projectSources = Sources.Where(source => source.ProjectKey == project.ProjectKey);
            foreach (var source in projectSources)
            {
                var groupEvents = events.Where(item =>
                    item.ProjectKey == source.ProjectKey && item.Group == source.Group).ToArray();
                if (source.ShowGroupHeading)
                {
                    builder.Append("### ").AppendLine(source.Group).AppendLine();
                }

                if (source.ProjectKey == "economy" && source.Group == "Accounts")
                {
                    builder.AppendLine("События `Loaded`, `LoadFailed`, `Saved` и `SaveFailed` выполняются из фоновой очереди БД. В их обработчиках нельзя обращаться к игровым entity/API без явного возврата в scheduler игрового потока.").AppendLine();
                }

                builder.AppendLine("| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |");
                builder.AppendLine("|---|---|---|---|---|---|");
                foreach (var item in groupEvents)
                {
                    builder.Append("| `").Append(item.ApiPath).Append("` | `")
                        .Append(item.Context).Append("`<br>")
                        .Append(string.Join("<br>", item.ContextParameters.Select(parameter => $"`{parameter.Name}: {parameter.Type}`{(parameter.Mutable ? " (mutable)" : "")}{(parameter.Nullable ? " (nullable)" : "")}")))
                        .Append(item.IsCancellable ? "<br>cancellable" : string.Empty).Append(" | ").Append(MarkdownCell(item.When))
                        .Append(" | ").Append(item.Frequency)
                        .Append(" | ").Append(item.Load)
                        .Append(" | ").Append(MarkdownCell(item.Risk)).AppendLine(" |");
                }

                builder.AppendLine();
            }
        }

        builder.AppendLine(footer.TrimEnd());
        return builder.ToString();
    }

    private static int Generate(string root, string markdown, string catalogJson)
    {
        WriteText(Path.Combine(root, "docs/events.md"), markdown);
        WriteText(Path.Combine(root, "docs/generated/events.json"), catalogJson);
        Console.WriteLine("Generated docs/events.md and docs/generated/events.json.");
        return 0;
    }

    private static int Check(string root, string markdown, string catalogJson)
    {
        var stale = new List<string>();
        CheckFile(Path.Combine(root, "docs/events.md"), markdown, stale);
        CheckFile(Path.Combine(root, "docs/generated/events.json"), catalogJson, stale);
        if (stale.Count == 0)
        {
            Console.WriteLine("Event documentation is complete and up to date.");
            return 0;
        }

        Console.Error.WriteLine("Generated documentation is stale: " + string.Join(", ", stale));
        Console.Error.WriteLine("Run: dotnet run --project tools/EventDocsGenerator -- generate");
        return 1;
    }

    private static int WritePackage(
        string root,
        CatalogDocument catalog,
        IReadOnlyDictionary<string, string> options)
    {
        var output = RequireOption(options, "output");
        var repository = RequireOption(options, "repository");
        var branch = RequireOption(options, "branch");
        var commit = RequireOption(options, "commit");
        var runId = RequireOption(options, "run-id");
        var sourceUrl = options.TryGetValue("source-url", out var configuredSourceUrl)
            ? configuredSourceUrl
            : $"https://github.com/{repository}/tree/{commit}";
        var package = new DocumentationPackage(
            1,
            PackageKind,
            DateTimeOffset.UtcNow,
            new PackageProject(
                "cs2-zombie-plague",
                "CS2 Zombie Plague",
                repository,
                branch,
                commit,
                runId,
                sourceUrl),
            catalog);

        var fullPath = Path.GetFullPath(output, root);
        WriteText(fullPath, Serialize(package));
        Console.WriteLine($"Created documentation package: {fullPath}");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> arguments)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using var enumerator = arguments.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var key = enumerator.Current;
            if (!key.StartsWith("--", StringComparison.Ordinal) || !enumerator.MoveNext())
            {
                throw new DocumentationException($"Invalid option '{key}'. Options use --name value.");
            }

            result[key[2..]] = enumerator.Current;
        }

        return result;
    }

    private static string RequireOption(IReadOnlyDictionary<string, string> options, string key)
    {
        if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new DocumentationException($"Missing required option --{key}.");
        }

        return value;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CS2ZombiePlague.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DocumentationException("Run the generator from the cs2-zombie-plague repository.");
    }

    private static string ReadTemplate(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new DocumentationException($"Template does not exist: {relativePath}");
        }

        return NormalizeNewLines(File.ReadAllText(path));
    }

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, NormalizeNewLines(content), new UTF8Encoding(false));
    }

    private static void CheckFile(string path, string expected, ICollection<string> stale)
    {
        if (!File.Exists(path) || NormalizeNewLines(File.ReadAllText(path)) != NormalizeNewLines(expected))
        {
            stale.Add(Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'));
        }
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions) + "\n";

    private static string NormalizeNewLines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string XmlText(XElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        var value = string.Concat(element.Nodes().Select(XmlNodeText));
        return WhitespaceRegex().Replace(value, " ").Trim();
    }

    private static string XmlNodeText(XNode node) => node switch
    {
        XText text => text.Value,
        XElement element when element.Name.LocalName == "c" => $"`{XmlText(element)}`",
        XElement element when element.Name.LocalName == "see" =>
            $"`{element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? string.Empty}`",
        XElement element => string.Concat(element.Nodes().Select(XmlNodeText)),
        _ => string.Empty,
    };

    private static string RiskLevel(string risk)
    {
        var separator = risk.IndexOf(':');
        return (separator >= 0 ? risk[..separator] : risk).Trim();
    }

    private static string MarkdownCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    [GeneratedRegex(
        @"(?<docs>(?:\s*///.*\r?\n)+)\s*(?<property>IHookSubscription<(?<context>[A-Za-z0-9_]+)>\s+(?<name>[A-Za-z0-9_]+)\s*\{\s*get;\s*\})",
        RegexOptions.Multiline)]
    private static partial Regex EventPropertyRegex();

    [GeneratedRegex(@"^\s*///\s?", RegexOptions.Multiline)]
    private static partial Regex XmlCommentPrefixRegex();

    [GeneratedRegex(@"IHookSubscription<[A-Za-z0-9_]+>\s+[A-Za-z0-9_]+\s*\{\s*get;\s*\}")]
    private static partial Regex SubscriptionRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"public\s+(?:readonly\s+)?(?:record\s+)?struct\s+(?<name>[A-Za-z0-9_]+)[^{:]*?(?<interfaces>:\s*[^\{]+)?\s*\{")]
    private static partial Regex ContextTypeRegex();

    [GeneratedRegex(@"public\s+(?<type>[A-Za-z0-9_<>.,?\[\]\s]+?)\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<accessor>[^{}]*)\}")]
    private static partial Regex ContextPropertyRegex();

    private sealed record SourceDefinition(
        string ProjectKey,
        string Project,
        string Group,
        bool ShowGroupHeading,
        string PublicPrefix,
        string RelativePath);

    private sealed record CatalogDocument(
        int SchemaVersion,
        string Kind,
        int EventCount,
        IReadOnlyList<CatalogProject> Projects,
        IReadOnlyList<EventDocumentation> Events);

    private sealed record CatalogProject(
        string Key,
        string Name,
        int EventCount,
        IReadOnlyList<CatalogGroup> Groups);

    private sealed record CatalogGroup(string Name, int EventCount);

    private sealed record EventDocumentation(
        string Id,
        string ProjectKey,
        string Project,
        string Group,
        string ApiPath,
        string Name,
        string Context,
        bool IsCancellable,
        IReadOnlyList<ContextParameter> ContextParameters,
        string Summary,
        string When,
        string Frequency,
        string Load,
        string RiskLevel,
        string Risk,
        string Thread,
        string SourcePath,
        int SourceLine);

    private sealed record ContextDocumentation(bool IsCancellable, IReadOnlyList<ContextParameter> Parameters, string SourcePath);
    private sealed record ContextParameter(string Name, string Type, bool Nullable, bool Mutable);

    private sealed record DocumentationPackage(
        int SchemaVersion,
        string Kind,
        DateTimeOffset GeneratedAt,
        PackageProject Project,
        CatalogDocument Catalog);

    private sealed record PackageProject(
        string Key,
        string Name,
        string Repository,
        string Branch,
        string Commit,
        string RunId,
        string SourceUrl);

    private sealed class DocumentationException(string message) : Exception(message)
    {
    }
}
