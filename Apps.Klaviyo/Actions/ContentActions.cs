using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Actions;

[ActionList("Content")]
public class ContentActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search content", Description = "Searches all content with translations.")]
    public Task<SearchTranslationsResponse> SearchContent([ActionParameter] SearchContentRequest input) =>
        new TranslationService(Client).SearchAsync(input, input.ContentTypes);
}
