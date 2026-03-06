using Hl7ToCcda.Core.Conversion;
using Hl7ToCcda.Core.Exceptions;

namespace Hl7ToCcda.Core.Tests;

public class Hl7RootTemplateDetectorTests
{
    private readonly Hl7RootTemplateDetector _detector = new();

    [Fact]
    public void Detect_UsesMessageStructureWhenPresent()
    {
        const string message = "MSH|^~\\&|SEND|FAC|RECV|FAC|20240101120000||ADT^A01^ADT_A01|MSG00001|P|2.5\rPID|1||12345^^^MRN^MR||Doe^Jane";

        string template = _detector.Detect(message);

        Assert.Equal("ADT_A01", template);
    }

    [Fact]
    public void Detect_FallsBackToMessageCodeAndTriggerEvent()
    {
        const string message = "MSH|^~\\&|SEND|FAC|RECV|FAC|20240101120000||ORU^R01|MSG00001|P|2.5\rPID|1||12345^^^MRN^MR||Doe^Jane";

        string template = _detector.Detect(message);

        Assert.Equal("ORU_R01", template);
    }

    [Fact]
    public void Detect_ThrowsForMissingMessageHeader()
    {
        const string message = "PID|1||12345^^^MRN^MR||Doe^Jane";

        Assert.Throws<InvalidHl7MessageException>(() => _detector.Detect(message));
    }
}
