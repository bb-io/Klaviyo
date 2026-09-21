using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class UniversalContentActionTests : TestBase
{
    private static readonly DateTime UpdatedFrom = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Search_universal_content_works_with_optional_filters()
    {
        var result = await new UniversalContentActions(InvocationContext, FileManager)
            .SearchUniversalContent(new SearchTranslationsRequest
            {
                Channels = [TranslationChannels.Email],
                UpdatedFrom = UpdatedFrom,
                UpdatedTo = DateTime.UtcNow.AddMinutes(5)
            });

        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item =>
            item.ResourceType == TranslationResourceTypes.UniversalContent));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom));
    }

    [TestMethod]
    public async Task Get_universal_content_works()
    {
        var actions = new UniversalContentActions(InvocationContext, FileManager);
        var translationId = Configuration["TestData:universalContentTranslationId"];
        if (string.IsNullOrWhiteSpace(translationId))
        {
            translationId = (await actions.SearchUniversalContent(new SearchTranslationsRequest()))
                .Items.FirstOrDefault()?.ContentId;
        }

        if (string.IsNullOrWhiteSpace(translationId))
            Assert.Inconclusive(
                "No universal content translation is available. Set TestData:universalContentTranslationId in appsettings.json.");

        var result = await actions.GetUniversalContent(
            new UniversalContentIdentifier { UniversalContentId = translationId! });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
    }
}
