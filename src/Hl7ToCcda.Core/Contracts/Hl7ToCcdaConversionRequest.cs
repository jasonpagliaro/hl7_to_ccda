using System;

namespace Hl7ToCcda.Core;

public sealed record Hl7ToCcdaConversionRequest(
    string Hl7Message,
    string? RootTemplateOverride = null,
    string? DocumentTitle = null,
    DateTimeOffset? EffectiveTime = null);
