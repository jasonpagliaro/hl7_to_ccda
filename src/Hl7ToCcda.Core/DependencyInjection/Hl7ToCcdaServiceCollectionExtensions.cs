using Hl7ToCcda.Core.Ccda;
using Hl7ToCcda.Core.Conversion;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7ToCcda.Core.DependencyInjection;

public static class Hl7ToCcdaServiceCollectionExtensions
{
    public static IServiceCollection AddHl7ToCcdaConversion(this IServiceCollection services)
    {
        services.AddSingleton<VendoredHl7TemplateStore>();
        services.AddSingleton<Hl7RootTemplateDetector>();
        services.AddSingleton<CcdaDocumentBuilder>();
        services.AddSingleton<CcdaXmlWriter>();
        services.AddSingleton<IHl7ToCcdaConverter, Hl7ToCcdaConverter>();

        return services;
    }
}
