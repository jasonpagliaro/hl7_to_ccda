using Hl7ToCcda.Core.Conversion;

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
}
