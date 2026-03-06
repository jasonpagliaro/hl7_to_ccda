using System.Reflection;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Hl7ToCcda.Core.Conversion;

internal sealed class VendoredHl7TemplateStore
{
    private const string TemplateResourcePrefix = "VendoredTemplates/Hl7v2/";
    private readonly Dictionary<string, string> _rootTemplateLookup;

    public VendoredHl7TemplateStore()
    {
        var rawTemplates = LoadRawTemplates();
        var parsedTemplates = CreateParsedTemplateStore(rawTemplates);
        TemplateProvider = new TemplateProvider([parsedTemplates]);
        _rootTemplateLookup = parsedTemplates.Keys
            .Where(key => !key.Contains('/', StringComparison.Ordinal))
            .ToDictionary(key => key.ToUpperInvariant(), key => key, StringComparer.OrdinalIgnoreCase);
    }

    public ITemplateProvider TemplateProvider { get; }

    public IReadOnlyCollection<string> AvailableRootTemplates => _rootTemplateLookup.Values;

    public bool TryResolveRootTemplate(string candidate, out string rootTemplate)
    {
        return _rootTemplateLookup.TryGetValue(candidate.Trim().ToUpperInvariant(), out rootTemplate!);
    }

    private static Dictionary<string, string> LoadRawTemplates()
    {
        Assembly assembly = typeof(VendoredHl7TemplateStore).Assembly;
        string[] resourceNames = assembly.GetManifestResourceNames();
        var templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string resourceName in resourceNames.Where(name => name.StartsWith(TemplateResourcePrefix, StringComparison.Ordinal)))
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            string relativePath = resourceName[TemplateResourcePrefix.Length..].Replace('\\', '/');
            templates[relativePath] = reader.ReadToEnd();
        }

        if (templates.Count == 0)
        {
            throw new InvalidOperationException("No embedded HL7 templates were found in the core assembly.");
        }

        return templates;
    }

    private static Dictionary<string, Template> CreateParsedTemplateStore(IReadOnlyDictionary<string, string> rawTemplates)
    {
        var parsedTemplates = new Dictionary<string, Template>(StringComparer.OrdinalIgnoreCase);

        foreach ((string relativePath, string content) in rawTemplates)
        {
            string normalizedRelativePath = relativePath.Replace('\\', '/');
            string formattedRelativePath = TemplateUtility.GetTemplateKey(NormalizeTemplateResourcePath(normalizedRelativePath))
                ?? string.Empty;
            string rawTemplateKey = TemplateUtility.GetTemplateKey(normalizedRelativePath)
                ?? string.Empty;

            if (string.IsNullOrEmpty(formattedRelativePath))
            {
                continue;
            }

            Template parsedTemplate = TemplateUtility.ParseTemplate(formattedRelativePath, content);
            parsedTemplates[formattedRelativePath] = parsedTemplate;

            if (!string.Equals(rawTemplateKey, formattedRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                parsedTemplates[rawTemplateKey] = parsedTemplate;
            }
        }

        return parsedTemplates;
    }

    private static string NormalizeTemplateResourcePath(string templatePath)
    {
        var normalizedTemplatePath = templatePath.Replace('\\', '/');
        var lastSlashIndex = normalizedTemplatePath.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex == normalizedTemplatePath.Length - 1)
        {
            return normalizedTemplatePath;
        }

        string prefix = normalizedTemplatePath[..(lastSlashIndex + 1)];
        string fileName = normalizedTemplatePath[(lastSlashIndex + 1)..];
        if (fileName.StartsWith('_') &&
            !fileName.StartsWith("_CodeSystem.", StringComparison.OrdinalIgnoreCase) &&
            !fileName.StartsWith("_ValueSet.", StringComparison.OrdinalIgnoreCase))
        {
            return prefix + fileName[1..];
        }

        return normalizedTemplatePath;
    }
}
