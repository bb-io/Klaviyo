using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Actions;

[ActionList("Templates")]
public class TemplateActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Search templates", Description = "Searches translations associated with templates.")]
    public Task<SearchTranslationsResponse> SearchTemplates(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.Template]);

    [Action("Get template", Description = "Gets a template associated with a translation.")]
    public Task<TemplateResponse> GetTemplate([ActionParameter] TranslationIdentifier input) =>
        new TranslationService(Client).GetTemplateAsync(input.TranslationId);
}
