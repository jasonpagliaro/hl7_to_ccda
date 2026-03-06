using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Hl7ToCcda.Core.Fhir;

internal static class FhirText
{
    private static readonly Regex DigitsOnly = new("[^0-9]", RegexOptions.Compiled);

    public static string? FirstIdentifier(JObject? resource)
    {
        string? fromIdentifier = resource?["identifier"]?.FirstOrDefault()?["value"]?.Value<string>();
        return FirstMeaningful(fromIdentifier, resource?["id"]?.Value<string>());
    }

    public static string? CodeableConcept(JToken? token)
    {
        return FirstMeaningful(
            token?["text"]?.Value<string>(),
            token?["coding"]?.FirstOrDefault()?["display"]?.Value<string>(),
            token?["coding"]?.FirstOrDefault()?["code"]?.Value<string>());
    }

    public static string? HumanName(JToken? token)
    {
        JToken? name = token is JArray array ? array.FirstOrDefault() : token;
        if (name is null)
        {
            return null;
        }

        IEnumerable<string?> given = name["given"]?.Values<string>().Select(value => (string?)value) ?? [];
        return JoinNonEmpty(given.Concat([name["family"]?.Value<string>()]));
    }

    public static string? Address(JToken? token)
    {
        JToken? address = token is JArray array ? array.FirstOrDefault() : token;
        if (address is null)
        {
            return null;
        }

        return FirstMeaningful(
            address["text"]?.Value<string>(),
            JoinNonEmpty((address["line"]?.Values<string>() ?? [])
                .Concat([address["city"]?.Value<string>(), address["state"]?.Value<string>(), address["postalCode"]?.Value<string>()])));
    }

    public static string? Telecom(JToken? token)
    {
        JToken? telecom = token is JArray array ? array.FirstOrDefault() : token;
        if (telecom is null)
        {
            return null;
        }

        string? system = telecom["system"]?.Value<string>();
        string? value = telecom["value"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(system) ? value : $"{system}: {value}";
    }

    public static string? ReferenceDisplay(JToken? token)
    {
        return FirstMeaningful(
            token?["display"]?.Value<string>(),
            token?["reference"]?.Value<string>());
    }

    public static string? DateOrDateTime(JToken? token)
    {
        string? literal = token?.Value<string>();
        if (string.IsNullOrWhiteSpace(literal))
        {
            return null;
        }

        return literal;
    }

    public static string? CdaBirthDate(JObject? patient)
    {
        string? birthDate = patient?["birthDate"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(birthDate))
        {
            return null;
        }

        string digits = DigitsOnly.Replace(birthDate, string.Empty);
        return digits.Length >= 8 ? digits[..8] : digits;
    }

    public static string CdaTimestamp(DateTimeOffset timestamp)
    {
        string baseTimestamp = timestamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        string offset = timestamp.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", string.Empty, StringComparison.Ordinal);
        return $"{baseTimestamp}{offset}";
    }

    public static string? Period(JToken? token)
    {
        string? start = token?["start"]?.Value<string>();
        string? end = token?["end"]?.Value<string>();
        return FirstMeaningful(
            JoinNonEmpty([start, end is null ? null : $"through {end}"]),
            start,
            end);
    }

    public static string? Quantity(JToken? token)
    {
        if (token is null)
        {
            return null;
        }

        string? value = token["value"]?.Value<string>() ?? token["value"]?.ToString();
        string? unit = token["unit"]?.Value<string>();
        return JoinNonEmpty([value, unit]);
    }

    public static string? GenderToAdministrativeCode(JObject? patient)
    {
        return patient?["gender"]?.Value<string>()?.ToLowerInvariant() switch
        {
            "male" => "M",
            "female" => "F",
            "other" => "UN",
            "unknown" => "UNK",
            _ => null,
        };
    }

    public static string JoinNonEmpty(IEnumerable<string?> parts)
    {
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    public static string? FirstMeaningful(params string?[] candidates)
    {
        return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim();
    }
}
