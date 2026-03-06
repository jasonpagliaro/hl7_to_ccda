using System.Xml.Linq;
using Hl7ToCcda.Core.DependencyInjection;
using Hl7ToCcda.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7ToCcda.Core.Tests;

public class Hl7ToCcdaConverterTests
{
    private static readonly DateTimeOffset FixedEffectiveTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("ADT-A01-01.hl7", "ADT-A01-01.xml")]
    [InlineData("ORU-R01-01.hl7", "ORU-R01-01.xml")]
    [InlineData("ORM-O01-01.hl7", "ORM-O01-01.xml")]
    [InlineData("SIU-S12-01.hl7", "SIU-S12-01.xml")]
    [InlineData("VXU-V04-01.hl7", "VXU-V04-01.xml")]
    public async Task ConvertAsync_MatchesExpectedSnapshot(string inputFileName, string expectedFileName)
    {
        IHl7ToCcdaConverter converter = CreateConverter();
        string hl7Message = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "vendor", "fhir-converter", "data", "SampleData", "Hl7v2", inputFileName));
        string expectedXml = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "tests", "Hl7ToCcda.Core.Tests", "TestData", "Expected", expectedFileName));

        Hl7ToCcdaConversionResult result = await converter.ConvertAsync(
            new Hl7ToCcdaConversionRequest(
                Hl7Message: hl7Message,
                DocumentTitle: "Test CCD",
                EffectiveTime: FixedEffectiveTime));

        Assert.Equal(NormalizeXml(expectedXml), NormalizeXml(result.CcdaXml));
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidHl7_Throws()
    {
        IHl7ToCcdaConverter converter = CreateConverter();

        await Assert.ThrowsAsync<InvalidHl7MessageException>(() =>
            converter.ConvertAsync(new Hl7ToCcdaConversionRequest("not-an-hl7-message")));
    }

    [Fact]
    public async Task ConvertAsync_WithoutMappableClinicalContent_Throws()
    {
        IHl7ToCcdaConverter converter = CreateConverter();
        const string message = "MSH|^~\\&|SEND|FAC|RECV|FAC|20240101120000||ADT^A01^ADT_A01|MSG00001|P|2.5\rPID|1||12345^^^MRN^MR||Doe^Jane";

        await Assert.ThrowsAsync<InsufficientClinicalContentException>(() =>
            converter.ConvertAsync(new Hl7ToCcdaConversionRequest(
                Hl7Message: message,
                RootTemplateOverride: "ADT_A01",
                DocumentTitle: "Test CCD",
                EffectiveTime: FixedEffectiveTime)));
    }

    private static IHl7ToCcdaConverter CreateConverter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHl7ToCcdaConversion();

        return services.BuildServiceProvider().GetRequiredService<IHl7ToCcdaConverter>();
    }

    private static string NormalizeXml(string xml)
    {
        return XDocument.Parse(xml).ToString(SaveOptions.DisableFormatting);
    }
}
