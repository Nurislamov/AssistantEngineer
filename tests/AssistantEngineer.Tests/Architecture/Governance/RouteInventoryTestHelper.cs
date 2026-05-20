using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Architecture.Governance;

internal static partial class RouteInventoryTestHelper
{
    private static readonly string[] HttpAttributeNames =
    [
        "HttpGet",
        "HttpPost",
        "HttpPut",
        "HttpDelete",
        "HttpPatch"
    ];

    public static IReadOnlyList<DiscoveredRouteEndpoint> DiscoverControllerEndpoints(string controllersRootPath)
    {
        var files = Directory.GetFiles(controllersRootPath, "*Controller*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(files);

        var classRoutesByController = DiscoverClassRoutes(files);
        var endpoints = new List<DiscoveredRouteEndpoint>();

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            var controller = TryGetControllerName(lines) ?? Path.GetFileNameWithoutExtension(file);
            var localClassRoute = TryGetClassRoute(lines);

            var pendingHttpAttributes = new List<(string Method, string? Template, int Line)>();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var httpMatch = HttpAttributeRegex().Match(line);
                if (httpMatch.Success)
                {
                    pendingHttpAttributes.Add((
                        Method: httpMatch.Groups["method"].Value.ToUpperInvariant(),
                        Template: ExtractQuotedTemplate(httpMatch.Groups["args"].Value),
                        Line: i + 1));
                    continue;
                }

                if (!LooksLikeMethodSignature(line) || pendingHttpAttributes.Count == 0)
                    continue;

                var actionName = TryGetActionName(line) ?? "UnknownNeedsClassification";
                var classRoutes = ResolveClassRoutesForController(classRoutesByController, controller, localClassRoute);
                foreach (var attribute in pendingHttpAttributes)
                {
                    foreach (var classRoute in classRoutes)
                    {
                        var route = CombineRoute(classRoute, attribute.Template);
                        endpoints.Add(new DiscoveredRouteEndpoint(
                            controller,
                            actionName,
                            attribute.Method,
                            route,
                            GovernancePathHelper.ToRepoRelative(file),
                            attribute.Line));
                    }
                }

                pendingHttpAttributes.Clear();
            }
        }

        return endpoints
            .DistinctBy(endpoint => endpoint.UniqueKey, StringComparer.Ordinal)
            .OrderBy(endpoint => endpoint.Controller, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.RouteTemplate, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.HttpMethod, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsEndpointRepresentedInInventory(
        JsonElement inventoryRoot,
        DiscoveredRouteEndpoint discovered)
    {
        var endpoints = inventoryRoot.GetProperty("endpoints").EnumerateArray();
        foreach (var entry in endpoints)
        {
            var controller = entry.GetProperty("controller").GetString() ?? string.Empty;
            if (!string.Equals(controller, discovered.Controller, StringComparison.Ordinal))
                continue;

            var routePattern = entry.GetProperty("routePattern").GetString() ?? string.Empty;
            if (string.Equals(routePattern, "UnknownNeedsAudit", StringComparison.Ordinal))
                return true;

            var inventoryRoute = NormalizeRoute(RemoveMethodSuffix(routePattern));
            var discoveredRoute = NormalizeRoute(discovered.RouteTemplate);
            if (!string.Equals(inventoryRoute, discoveredRoute, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!entry.TryGetProperty("httpMethod", out var methodElement))
                return true;

            var method = methodElement.GetString() ?? string.Empty;
            if (string.Equals(method, discovered.HttpMethod, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "MULTI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "ANY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "UnknownNeedsClassification", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<RouteInventoryIgnoreEntry> LoadIgnoreEntries(string ignoreListPath)
    {
        if (!File.Exists(ignoreListPath))
            return [];

        using var document = JsonDocument.Parse(File.ReadAllText(ignoreListPath));
        if (!document.RootElement.TryGetProperty("entries", out var entriesElement) ||
            entriesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<RouteInventoryIgnoreEntry>();
        foreach (var entry in entriesElement.EnumerateArray())
        {
            var controller = entry.TryGetProperty("controller", out var controllerElement)
                ? controllerElement.GetString() ?? string.Empty
                : string.Empty;
            var method = entry.TryGetProperty("method", out var methodElement)
                ? methodElement.GetString() ?? string.Empty
                : string.Empty;
            var routeContains = entry.TryGetProperty("routeContains", out var routeElement)
                ? routeElement.GetString() ?? string.Empty
                : string.Empty;
            var reason = entry.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString() ?? string.Empty
                : string.Empty;

            entries.Add(new RouteInventoryIgnoreEntry(controller, method, routeContains, reason));
        }

        return entries;
    }

    public static bool IsIgnored(DiscoveredRouteEndpoint endpoint, IReadOnlyList<RouteInventoryIgnoreEntry> ignores)
    {
        foreach (var ignore in ignores)
        {
            var controllerMatches = string.IsNullOrWhiteSpace(ignore.Controller) ||
                                    string.Equals(ignore.Controller, endpoint.Controller, StringComparison.Ordinal);
            if (!controllerMatches)
                continue;

            var methodMatches = string.IsNullOrWhiteSpace(ignore.Method) ||
                                string.Equals(ignore.Method, endpoint.HttpMethod, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(ignore.Method, "*", StringComparison.Ordinal);
            if (!methodMatches)
                continue;

            var routeMatches = string.IsNullOrWhiteSpace(ignore.RouteContains) ||
                               endpoint.RouteTemplate.Contains(ignore.RouteContains, StringComparison.OrdinalIgnoreCase);
            if (!routeMatches)
                continue;

            return true;
        }

        return false;
    }

    private static Dictionary<string, HashSet<string>> DiscoverClassRoutes(IReadOnlyList<string> files)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            var controller = TryGetControllerName(lines);
            var classRoute = TryGetClassRoute(lines);
            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(classRoute))
                continue;

            if (!map.TryGetValue(controller, out var routes))
            {
                routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[controller] = routes;
            }

            routes.Add(classRoute);
        }

        return map;
    }

    private static string[] ResolveClassRoutesForController(
        IReadOnlyDictionary<string, HashSet<string>> classRoutesByController,
        string controller,
        string? localClassRoute)
    {
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(localClassRoute))
            routes.Add(localClassRoute);

        if (classRoutesByController.TryGetValue(controller, out var globalRoutes))
            routes.UnionWith(globalRoutes);

        if (routes.Count == 0)
            routes.Add("UnknownNeedsClassification");

        return routes.ToArray();
    }

    private static string? TryGetControllerName(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var match = ControllerDeclarationRegex().Match(line);
            if (match.Success)
                return match.Groups["name"].Value;
        }

        return null;
    }

    private static string? TryGetClassRoute(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var classMatch = ControllerDeclarationRegex().Match(lines[i]);
            if (!classMatch.Success)
                continue;

            for (var j = i - 1; j >= Math.Max(0, i - 12); j--)
            {
                var routeMatch = RouteAttributeRegex().Match(lines[j]);
                if (routeMatch.Success)
                    return routeMatch.Groups["template"].Value;

                if (!string.IsNullOrWhiteSpace(lines[j]) && !lines[j].TrimStart().StartsWith("[", StringComparison.Ordinal))
                    break;
            }
        }

        return null;
    }

    private static bool LooksLikeMethodSignature(string line)
    {
        if (!line.Contains('(') || !line.Contains("public", StringComparison.Ordinal))
            return false;

        var trimmed = line.TrimStart();
        return !trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string? TryGetActionName(string line)
    {
        var match = MethodNameRegex().Match(line);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string CombineRoute(string classRoute, string? methodTemplate)
    {
        if (string.Equals(classRoute, "UnknownNeedsClassification", StringComparison.OrdinalIgnoreCase))
            return "UnknownNeedsClassification";

        var prefix = classRoute.Trim();
        var suffix = methodTemplate?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(suffix))
            return NormalizeRoute(prefix);

        if (suffix.StartsWith("~/", StringComparison.Ordinal))
            return NormalizeRoute(suffix[2..]);

        if (suffix.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            return NormalizeRoute(suffix);

        if (suffix.StartsWith("/", StringComparison.Ordinal))
            return NormalizeRoute($"{prefix.TrimEnd('/')}{suffix}");

        return NormalizeRoute($"{prefix.TrimEnd('/')}/{suffix.TrimStart('/')}");
    }

    private static string RemoveMethodSuffix(string routePattern)
    {
        return routePattern.Replace(" [GET]", string.Empty, StringComparison.Ordinal)
            .Replace(" [POST]", string.Empty, StringComparison.Ordinal)
            .Replace(" [PUT]", string.Empty, StringComparison.Ordinal)
            .Replace(" [DELETE]", string.Empty, StringComparison.Ordinal)
            .Replace(" [PATCH]", string.Empty, StringComparison.Ordinal)
            .Replace(" [MULTI]", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizeRoute(string route)
    {
        return route.Replace("\\", "/")
            .Replace("//", "/")
            .Trim();
    }

    private static string? ExtractQuotedTemplate(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return null;

        var match = QuotedStringRegex().Match(rawArgs);
        return match.Success ? match.Groups["value"].Value : null;
    }

    [GeneratedRegex(@"class\s+(?<name>\w+Controller)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ControllerDeclarationRegex();

    [GeneratedRegex(@"\[Route\(""(?<template>[^""]+)""\)\]", RegexOptions.CultureInvariant)]
    private static partial Regex RouteAttributeRegex();

    [GeneratedRegex(@"\[Http(?<method>Get|Post|Put|Delete|Patch)(?:\((?<args>[^)]*)\))?\]", RegexOptions.CultureInvariant)]
    private static partial Regex HttpAttributeRegex();

    [GeneratedRegex(@"public\s+(?:async\s+)?[\w<>\[\],\s]+\s+(?<name>\w+)\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex MethodNameRegex();

    [GeneratedRegex(@"""(?<value>[^""]*)""", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedStringRegex();
}

internal sealed record DiscoveredRouteEndpoint(
    string Controller,
    string Action,
    string HttpMethod,
    string RouteTemplate,
    string SourcePath,
    int SourceLine)
{
    public string UniqueKey => $"{Controller}|{HttpMethod}|{RouteTemplate}";
}

internal sealed record RouteInventoryIgnoreEntry(
    string Controller,
    string Method,
    string RouteContains,
    string Reason);
