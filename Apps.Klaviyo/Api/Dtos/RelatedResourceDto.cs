using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Api.Dtos;

public class RelatedResourceDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("attributes")]
    public JObject Attributes { get; set; } = new();
}
