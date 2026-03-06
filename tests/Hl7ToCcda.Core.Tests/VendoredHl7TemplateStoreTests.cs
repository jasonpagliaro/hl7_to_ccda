using Hl7ToCcda.Core.Conversion;
using DotLiquid;
using DotLiquid.FileSystems;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using System.Globalization;
using System.Threading;

namespace Hl7ToCcda.Core.Tests;

public class VendoredHl7TemplateStoreTests
{
    [Fact]
    public void AvailableRootTemplates_ContainsCommonHl7Templates()
    {
        var store = new VendoredHl7TemplateStore();

        Assert.Contains("ADT_A01", store.AvailableRootTemplates);
        Assert.Contains("ORU_R01", store.AvailableRootTemplates);
        Assert.Contains("SIU_S12", store.AvailableRootTemplates);
    }

    [Fact]
    public void TryResolveRootTemplate_IsCaseInsensitive()
    {
        var store = new VendoredHl7TemplateStore();

        bool resolved = store.TryResolveRootTemplate("oru_r01", out string rootTemplate);

        Assert.True(resolved);
        Assert.Equal("ORU_R01", rootTemplate);
    }

    [Theory]
    [InlineData("Reference/Coverage/Beneficiary")]
    [InlineData("Reference/Coverage/_Beneficiary")]
    [InlineData("'Reference/Coverage/Beneficiary'")]
    [InlineData("\"Reference/Coverage/Beneficiary\"")]
    public void TemplateFileSystem_ResolvesCoverageBeneficiarySnippetVariants(string templateName)
    {
        var store = new VendoredHl7TemplateStore();
        var fileSystem = Assert.IsAssignableFrom<ITemplateFileSystem>(store.TemplateProvider.GetTemplateFileSystem());
        var context = CreateContext(fileSystem);

        Template? template = fileSystem.GetTemplate(context, templateName);

        Assert.NotNull(template);
    }

    [Fact]
    public void TemplateFileSystem_ResolvesTemplateFromContextVariable()
    {
        var store = new VendoredHl7TemplateStore();
        var fileSystem = Assert.IsAssignableFrom<ITemplateFileSystem>(store.TemplateProvider.GetTemplateFileSystem());
        var context = CreateContext(fileSystem);
        context["coverageTemplate"] = "Reference/Coverage/Beneficiary";

        Template? template = fileSystem.GetTemplate(context, "coverageTemplate");

        Assert.NotNull(template);
    }

    [Fact]
    public void IncludeRender_UsesCoverageBeneficiarySnippet()
    {
        var store = new VendoredHl7TemplateStore();
        var fileSystem = Assert.IsAssignableFrom<ITemplateFileSystem>(store.TemplateProvider.GetTemplateFileSystem());
        var context = CreateContext(fileSystem);
        var template = Template.Parse("{% include 'Reference/Coverage/Beneficiary' ID: ID, REF: REF -%}");

        string output = template.Render(RenderParameters.FromContext(context, CultureInfo.InvariantCulture));

        Assert.Contains("\"beneficiary\"", output);
        Assert.Contains("\"reference\":\"Patient/123\"", output);
    }

    [Theory]
    [InlineData("VendoredTemplates/Hl7v2/Reference/Coverage/_Beneficiary.liquid", "Reference/Coverage/_Beneficiary.liquid")]
    [InlineData("VendoredTemplates.Hl7v2.Reference.Coverage._Beneficiary.liquid", "Reference/Coverage/_Beneficiary.liquid")]
    [InlineData("Hl7ToCcda.Core.VendoredTemplates.Hl7v2.Reference.Coverage._Beneficiary.liquid", "Reference/Coverage/_Beneficiary.liquid")]
    [InlineData("VendoredTemplates.Hl7v2.schema.Patient.schema.json", "schema/Patient.schema.json")]
    public void TryGetTemplateRelativePath_SupportsSlashAndDotSeparatedManifestNames(string resourceName, string expectedRelativePath)
    {
        bool resolved = VendoredHl7TemplateStore.TryGetTemplateRelativePath(resourceName, out string relativePath);

        Assert.True(resolved);
        Assert.Equal(expectedRelativePath, relativePath);
    }

    private static Context CreateContext(ITemplateFileSystem fileSystem)
    {
        var context = new Context(
            environments: [Hash.FromDictionary(new Dictionary<string, object> { ["ID"] = "coverage-1", ["REF"] = "Patient/123" })],
            outerScope: new Hash(),
            registers: Hash.FromDictionary(new Dictionary<string, object> { ["file_system"] = fileSystem }),
            errorsOutputMode: ErrorsOutputMode.Rethrow,
            maxIterations: 0,
            formatProvider: CultureInfo.InvariantCulture,
            cancellationToken: CancellationToken.None);

        context[TemplateUtility.RootTemplateParentPathScope] = string.Empty;
        return context;
    }
}
