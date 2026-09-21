using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class FlowMessageActionTests : TestBase
{
    private static readonly DateTime UpdatedFrom = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Search_flow_messages_works_with_optional_filters()
    {
        var result = await new FlowMessageActions(InvocationContext, FileManager)
            .SearchFlowMessages(new SearchTranslationsRequest
            {
                Channels = [TranslationChannels.Email],
                UpdatedFrom = UpdatedFrom,
                UpdatedTo = DateTime.UtcNow.AddMinutes(5)
            });

        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item => item.ResourceType == TranslationResourceTypes.FlowMessage));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom));
    }
}
