using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class ContentActionTests : TestBase
{
    [TestMethod]
    public async Task Search_content_works_with_optional_filters()
    {
        var updatedFrom = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedTo = DateTime.UtcNow.AddMinutes(5);
        var result = await new ContentActions(InvocationContext, FileManager).SearchContent(
            new SearchContentRequest
            {
                ContentTypes =
                [
                    TranslationResourceTypes.Template,
                    TranslationResourceTypes.FlowMessage
                ],
                Channels = [TranslationChannels.Email],
                UpdatedFrom = updatedFrom,
                UpdatedTo = updatedTo
            });

        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item =>
            item.ResourceType is TranslationResourceTypes.Template or TranslationResourceTypes.FlowMessage));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= updatedFrom && item.Updated <= updatedTo));
    }
}
