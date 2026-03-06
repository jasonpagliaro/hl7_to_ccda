using Microsoft.AspNetCore.Http;

namespace Hl7ToCcda.Api.Contracts;

public sealed class ConvertCcdForm
{
    public IFormFile? File { get; init; }

    public string? Message { get; init; }

    public string? RootTemplateOverride { get; init; }

    public string? DocumentTitle { get; init; }
}
