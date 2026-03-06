using Hl7ToCcda.Api.Contracts;
using Hl7ToCcda.Core;
using Hl7ToCcda.Core.DependencyInjection;
using Hl7ToCcda.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHl7ToCcdaConversion();

var app = builder.Build();

app.UseCors("WebClient");

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/convert/ccd", async (
    [FromForm] ConvertCcdForm form,
    IHl7ToCcdaConverter converter,
    CancellationToken cancellationToken) =>
{
    string? hl7Message = null;

    if (form.File is { Length: > 0 })
    {
        using var reader = new StreamReader(form.File.OpenReadStream());
        hl7Message = await reader.ReadToEndAsync(cancellationToken);
    }
    else if (!string.IsNullOrWhiteSpace(form.Message))
    {
        hl7Message = form.Message;
    }

    if (string.IsNullOrWhiteSpace(hl7Message))
    {
        return Results.BadRequest(new ApiErrorResponse("MissingInput", "Provide either a file upload or a raw HL7 message."));
    }

    try
    {
        Hl7ToCcdaConversionResult result = await converter.ConvertAsync(
            new Hl7ToCcdaConversionRequest(
                Hl7Message: hl7Message,
                RootTemplateOverride: form.RootTemplateOverride,
                DocumentTitle: form.DocumentTitle),
            cancellationToken);

        return Results.Ok(new
        {
            fileName = $"{result.DetectedRootTemplate}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.ccda.xml",
            detectedRootTemplate = result.DetectedRootTemplate,
            warnings = result.Warnings,
            ccdaXml = result.CcdaXml,
        });
    }
    catch (InvalidHl7MessageException ex)
    {
        return Results.BadRequest(new ApiErrorResponse(ex.ErrorCode, ex.Message));
    }
    catch (UnsupportedMessageTypeException ex)
    {
        return Results.BadRequest(new ApiErrorResponse(ex.ErrorCode, ex.Message));
    }
    catch (InsufficientClinicalContentException ex)
    {
        return Results.Json(new ApiErrorResponse(ex.ErrorCode, ex.Message), statusCode: StatusCodes.Status422UnprocessableEntity);
    }
})
.DisableAntiforgery();

app.Run();

public partial class Program;
