using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class HandlerTests : TestBase
{
    [TestMethod]
    public async Task Dynamic_handler_works()
    {
        var handler = new DynamicHandler(InvocationContext);

        var result = await handler.GetDataAsync(new DataSourceContext { }, CancellationToken.None);

        Console.WriteLine($"Total: {result.Count()}");
        foreach (var item in result)
            Console.WriteLine($"{item.Value}: {item.DisplayName}");

        Assert.IsTrue(result.Count() > 0);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Campaign_variation_handler_uses_campaign_messages_endpoint()
    {
        var handler = new CampaignVariationDataHandler(InvocationContext);

        var result = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        Assert.IsNotNull(result);
    }
}
