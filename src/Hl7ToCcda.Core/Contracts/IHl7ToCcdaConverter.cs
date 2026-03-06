using System.Threading;
using System.Threading.Tasks;

namespace Hl7ToCcda.Core;

public interface IHl7ToCcdaConverter
{
    Task<Hl7ToCcdaConversionResult> ConvertAsync(
        Hl7ToCcdaConversionRequest request,
        CancellationToken cancellationToken = default);
}
