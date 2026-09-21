using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Api.Dtos;

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
