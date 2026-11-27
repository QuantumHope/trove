using Trove.UI;

namespace Trove.Tests;

public sealed class CoraProblemsTests
{
    [Fact]
    public void The_modules_own_reason_is_shown_as_written()
    {
        Assert.Equal(
            "CORA couldn't be reached. Try again in a moment.",
            CoraProblems.GatewayReason("""{"title":"Bad Gateway","status":502,"detail":"CORA couldn't be reached. Try again in a moment."}"""));
    }

    [Theory]
    [InlineData("""{"title":"The Trove module could not be reached.","detail":"Connection refused (localhost:5199)"}""")]
    [InlineData("""{"title":"The Trove module refused CORA's credentials.","detail":"Check that Modules:ServiceKey holds the same value."}""")]
    [InlineData("""{"title":"Bad Gateway","detail":42}""")]
    [InlineData("""<html>Bad gateway</html>""")]
    [InlineData("""[]""")]
    [InlineData("")]
    public void Anything_else_falls_back_to_the_usual_sentence(string body)
    {
        Assert.Null(CoraProblems.GatewayReason(body));
    }
}
