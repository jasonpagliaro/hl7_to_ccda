using Hl7ToCcda.Core.Exceptions;

namespace Hl7ToCcda.Core.Conversion;

internal sealed class Hl7RootTemplateDetector
{
    public string Detect(string hl7Message)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
        {
            throw new InvalidHl7MessageException("The HL7 message is empty.");
        }

        string? headerSegment = hl7Message
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith("MSH", StringComparison.Ordinal));

        if (headerSegment is null || headerSegment.Length < 8)
        {
            throw new InvalidHl7MessageException("The HL7 message does not contain a valid MSH segment.");
        }

        char fieldSeparator = headerSegment[3];
        char componentSeparator = headerSegment[4];
        string[] fields = headerSegment.Split(fieldSeparator);

        if (fields.Length <= 8 || string.IsNullOrWhiteSpace(fields[8]))
        {
            throw new InvalidHl7MessageException("The HL7 message is missing MSH-9, so the root template cannot be detected.");
        }

        string[] components = fields[8]
            .Split(componentSeparator)
            .Select(component => component.Trim())
            .ToArray();

        string? rootTemplate = null;

        if (components.Length >= 3 && !string.IsNullOrWhiteSpace(components[2]))
        {
            rootTemplate = components[2];
        }
        else if (components.Length >= 2 && !string.IsNullOrWhiteSpace(components[0]) && !string.IsNullOrWhiteSpace(components[1]))
        {
            rootTemplate = $"{components[0]}_{components[1]}";
        }

        if (string.IsNullOrWhiteSpace(rootTemplate))
        {
            throw new InvalidHl7MessageException("The HL7 message contains an unusable MSH-9 value.");
        }

        return rootTemplate.Trim().ToUpperInvariant();
    }
}
