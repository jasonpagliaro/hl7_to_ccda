using Hl7ToCcda.Core.Ccda;
using Hl7ToCcda.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hl7ToCcda.Core.Conversion;

internal sealed class Hl7ToCcdaConverter : IHl7ToCcdaConverter
{
    private readonly CcdaDocumentBuilder _documentBuilder;
    private readonly CcdaXmlWriter _xmlWriter;
    private readonly Hl7RootTemplateDetector _rootTemplateDetector;
    private readonly VendoredHl7TemplateStore _templateStore;
    private readonly ILoggerFactory _loggerFactory;

    public Hl7ToCcdaConverter(
        CcdaDocumentBuilder documentBuilder,
        CcdaXmlWriter xmlWriter,
        Hl7RootTemplateDetector rootTemplateDetector,
        VendoredHl7TemplateStore templateStore,
        ILoggerFactory loggerFactory)
    {
        _documentBuilder = documentBuilder;
        _xmlWriter = xmlWriter;
        _rootTemplateDetector = rootTemplateDetector;
        _templateStore = templateStore;
        _loggerFactory = loggerFactory;
    }

    public Task<Hl7ToCcdaConversionResult> ConvertAsync(
        Hl7ToCcdaConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Hl7Message))
        {
            throw new InvalidHl7MessageException("The HL7 message is empty.");
        }

        string requestedTemplate = request.RootTemplateOverride?.Trim() is { Length: > 0 } explicitTemplate
            ? explicitTemplate
            : _rootTemplateDetector.Detect(request.Hl7Message);

        if (!_templateStore.TryResolveRootTemplate(requestedTemplate, out string rootTemplate))
        {
            throw new UnsupportedMessageTypeException($"The HL7 message type '{requestedTemplate}' is not supported by the vendored template set.");
        }

        string fhirJson = ConvertHl7ToFhir(request.Hl7Message, rootTemplate, cancellationToken);
        var warnings = new List<ConversionWarning>();
        JObject bundle;

        try
        {
            bundle = JObject.Parse(fhirJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidHl7MessageException("The HL7 message converted to invalid FHIR JSON.", ex);
        }

        CcdaDocument document = _documentBuilder.Build(bundle, request, warnings);
        string xml = _xmlWriter.Write(document);

        return Task.FromResult(new Hl7ToCcdaConversionResult(
            CcdaXml: xml,
            DetectedRootTemplate: rootTemplate,
            Warnings: warnings.AsReadOnly()));
    }

    private string ConvertHl7ToFhir(string hl7Message, string rootTemplate, CancellationToken cancellationToken)
    {
        var processor = new Hl7v2Processor(new ProcessorSettings(), _loggerFactory.CreateLogger<Hl7v2Processor>());

        try
        {
            return processor.Convert(hl7Message, rootTemplate, _templateStore.TemplateProvider, cancellationToken);
        }
        catch (DataParseException ex)
        {
            throw new InvalidHl7MessageException(ex.Message, ex);
        }
        catch (FhirConverterException ex)
        {
            throw new InvalidHl7MessageException($"FHIR conversion failed: {ex.Message}", ex);
        }
    }
}
