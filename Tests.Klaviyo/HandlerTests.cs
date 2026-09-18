using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class HandlerTests : TestBase
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task Campaign_variation_handler_uses_campaign_messages_endpoint()
    {
        var handler = new CampaignVariationDataHandler(InvocationContext);

        var result = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

        Assert.IsNotNull(result);
    }
}
