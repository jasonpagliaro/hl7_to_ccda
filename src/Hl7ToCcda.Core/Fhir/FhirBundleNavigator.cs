using Newtonsoft.Json.Linq;

namespace Hl7ToCcda.Core.Fhir;

internal sealed class FhirBundleNavigator
{
    private readonly Dictionary<string, List<JObject>> _resourcesByType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JObject> _resourcesByReference = new(StringComparer.OrdinalIgnoreCase);

    public FhirBundleNavigator(JObject bundle)
    {
        if (!string.Equals(bundle["resourceType"]?.Value<string>(), "Bundle", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The converted FHIR payload is not a Bundle resource.");
        }

        foreach (JObject resource in bundle["entry"]?
                     .Children<JObject>()
                     .Select(entry => entry["resource"])
                     .OfType<JObject>() ?? [])
        {
            string? resourceType = resource["resourceType"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                continue;
            }

            if (!_resourcesByType.TryGetValue(resourceType, out List<JObject>? resources))
            {
                resources = [];
                _resourcesByType[resourceType] = resources;
            }

            resources.Add(resource);

            string? id = resource["id"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(id))
            {
                _resourcesByReference[$"{resourceType}/{id}"] = resource;
            }
        }
    }

    public IReadOnlyList<JObject> GetResources(string resourceType)
    {
        return _resourcesByType.TryGetValue(resourceType, out List<JObject>? resources) ? resources : [];
    }

    public JObject? First(string resourceType)
    {
        return GetResources(resourceType).FirstOrDefault();
    }

    public JObject? ResolveReference(JToken? referenceToken)
    {
        string? reference = referenceToken?["reference"]?.Value<string>() ?? referenceToken?.Value<string>();

        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return _resourcesByReference.TryGetValue(reference, out JObject? resource) ? resource : null;
    }
}
