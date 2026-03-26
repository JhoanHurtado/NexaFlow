using Xunit;
using Amazon.Lambda.TestUtilities;
using System.Net;

namespace NexaFlow.NexaBook.Tests;

public class FunctionsTest
{
    [Fact]
    public async Task TestGetMethod()
    {
        Functions functions = new Functions();
        var context = new TestLambdaContext();


        var ip = await functions.GetCallingIPAsync(context);

        Assert.True(IPAddress.TryParse(ip, out var _));
    }
}