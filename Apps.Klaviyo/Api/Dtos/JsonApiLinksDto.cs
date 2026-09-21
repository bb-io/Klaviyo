using Newtonsoft.Json;

namespace Apps.Klaviyo.Api.Dtos;

public class JsonApiLinksDto
{
    [JsonProperty("next")]
    public string? Next { get; set; }
}
