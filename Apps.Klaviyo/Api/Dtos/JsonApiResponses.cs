using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Api.Dtos;

public class JsonApiListResponse<T>
{
    [JsonProperty("data")]
    public List<T> Data { get; set; } = [];

    [JsonProperty("included")]
    public List<RelatedResourceDto> Included { get; set; } = [];

    [JsonProperty("links")]
    public JsonApiLinksDto? Links { get; set; }
}

public class JsonApiSingleResponse<T>
{
    [JsonProperty("data")]
    public T Data { get; set; } = default!;
}

public class JsonApiLinksDto
{
    [JsonProperty("next")]
    public string? Next { get; set; }
}

public class TranslationDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("attributes")]
    public TranslationAttributesDto Attributes { get; set; } = new();

    [JsonProperty("relationships")]
    public JObject Relationships { get; set; } = new();
}

public class TranslationAttributesDto
{
    [JsonProperty("source_locale")]
    public string? SourceLocale { get; set; }

    [JsonProperty("target_locales")]
    public List<string> TargetLocales { get; set; } = [];

    [JsonProperty("fallback_locale")]
    public string? FallbackLocale { get; set; }

    [JsonProperty("channel")]
    public string? Channel { get; set; }

    [JsonProperty("metadata")]
    public JToken? Metadata { get; set; }

    [JsonProperty("created")]
    public DateTimeOffset? Created { get; set; }

    [JsonProperty("updated")]
    public DateTimeOffset? Updated { get; set; }

    [JsonProperty("values")]
    public List<TranslationValueDto> Values { get; set; } = [];
}

public class TranslationValueDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("source_value")]
    public string SourceValue { get; set; } = string.Empty;

    [JsonProperty("translations")]
    public Dictionary<string, string> Translations { get; set; } = [];
}

public class RelatedResourceDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("attributes")]
    public JObject Attributes { get; set; } = new();
}
