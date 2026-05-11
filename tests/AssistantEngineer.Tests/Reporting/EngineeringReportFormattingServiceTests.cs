using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportFormattingServiceTests
{
    [Fact]
    public void Fixed2Formatting_IsDeterministicForNumericValues()
    {
        var service = new EngineeringReportFormattingService();

        Assert.Equal(12.345.ToString("F2"), service.FormatFixed2(12.345));
        Assert.Equal(0d.ToString("F2"), service.FormatFixed2(0));
        Assert.Equal((-5.5).ToString("F2"), service.FormatFixed2(-5.5));
    }

    [Fact]
    public void NullableFormatting_UsesFallbackForNull()
    {
        var service = new EngineeringReportFormattingService();

        Assert.Equal("n/a", service.FormatNullableFixed2(null));
        Assert.Equal(42d.ToString("F2"), service.FormatNullableFixed2(42));
    }
}
