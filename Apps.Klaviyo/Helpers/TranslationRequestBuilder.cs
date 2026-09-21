using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Helpers;

public static class TranslationRequestBuilder
{
    public static RestRequest FindByResourceId(string resourceId, int pageSize = 100) =>
        new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{resourceId}\")")
            .AddQueryParameter("page[size]", pageSize.ToString());

    public static RestRequest GetWithValues(string translationId) =>
        new RestRequest("translations/{translationId}", Method.Get)
            .AddUrlSegment("translationId", translationId)
            .AddQueryParameter("additional-fields[translation]", "values");

    public static RestRequest Create(
        string resourceType,
        string resourceId,
        string sourceLocale,
        string targetLocale,
        string channel)
    {
        var body = new
        {
            data = new
            {
                type = "translation",
                attributes = new
                {
                    source_locale = sourceLocale,
                    target_locales = new[] { targetLocale },
                    fallback_locale = sourceLocale,
                    channel
                },
                relationships = new Dictionary<string, object>
                {
                    [resourceType] = new { data = new { type = resourceType, id = resourceId } }
                }
            }
        };

        return new RestRequest("translations", Method.Post)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
    }

    public static RestRequest UpdateValues(
        string translationId,
        IReadOnlyDictionary<string, string> uploadedValues,
        string locale,
        IEnumerable<string>? targetLocales = null)
    {
        var attributes = new JObject
        {
            ["values"] = TranslationValuesHelper.BuildTranslatedValues(uploadedValues, locale)
        };
        if (targetLocales is not null)
            attributes["target_locales"] = JArray.FromObject(targetLocales);

        var body = new JObject
        {
            ["data"] = new JObject
            {
                ["type"] = "translation",
                ["id"] = translationId,
                ["attributes"] = attributes
            }
        };

        return new RestRequest("translations/{translationId}", Method.Patch)
            .AddUrlSegment("translationId", translationId)
            .AddStringBody(body.ToString(Formatting.None), "application/vnd.api+json");
    }

    public static RestRequest UpdateTargetLocales(
        string translationId,
        IEnumerable<string> targetLocales)
    {
        var body = new
        {
            data = new
            {
                type = "translation",
                id = translationId,
                attributes = new { target_locales = targetLocales }
            }
        };

        return new RestRequest("translations/{translationId}", Method.Patch)
            .AddUrlSegment("translationId", translationId)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
    }
}
