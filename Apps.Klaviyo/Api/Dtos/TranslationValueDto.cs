using Newtonsoft.Json;

namespace Apps.Klaviyo.Api.Dtos;

public class TranslationValueDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("source_value")]
    public string SourceValue { get; set; } = string.Empty;

    [JsonProperty("translations")]
    public Dictionary<string, string> Translations { get; set; } = [];
}
