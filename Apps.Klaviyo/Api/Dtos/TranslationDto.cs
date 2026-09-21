using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Api.Dtos;

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
