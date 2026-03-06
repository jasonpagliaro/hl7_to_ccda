using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;

namespace Hl7ToCcda.Api.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
        JObject payload = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", payload["status"]?.Value<string>());
    }

    [Fact]
    public async Task Convert_WithInvalidInput_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent("not-an-hl7-message"), "message" },
        };

        HttpResponseMessage response = await _client.PostAsync("/api/convert/ccd", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Convert_WithInsufficientContent_ReturnsUnprocessableEntity()
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent("MSH|^~\\&|SEND|FAC|RECV|FAC|20240101120000||ADT^A01^ADT_A01|MSG00001|P|2.5\rPID|1||12345^^^MRN^MR||Doe^Jane"), "message" },
            { new StringContent("ADT_A01"), "rootTemplateOverride" },
        };

        HttpResponseMessage response = await _client.PostAsync("/api/convert/ccd", content);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task Convert_WithUploadedHl7File_ReturnsCcdaJsonPayload()
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(Path.Combine(TestPaths.RepoRoot, "vendor", "fhir-converter", "data", "SampleData", "Hl7v2", "ORU-R01-01.hl7"));
        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

        using var content = new MultipartFormDataContent
        {
            { fileContent, "file", "ORU-R01-01.hl7" },
            { new StringContent("Test CCD"), "documentTitle" },
        };

        HttpResponseMessage response = await _client.PostAsync("/api/convert/ccd", content);

        response.EnsureSuccessStatusCode();

        JObject payload = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ORU_R01", payload["detectedRootTemplate"]?.Value<string>());
        Assert.Contains("<ClinicalDocument", payload["ccdaXml"]?.Value<string>());
        Assert.EndsWith(".ccda.xml", payload["fileName"]?.Value<string>());
        Assert.NotNull(payload["warnings"]);
    }
}
