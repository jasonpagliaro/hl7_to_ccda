using System.Collections.Generic;

namespace Hl7ToCcda.Core;

public sealed record Hl7ToCcdaConversionResult(
    string CcdaXml,
    string DetectedRootTemplate,
    IReadOnlyList<ConversionWarning> Warnings);
