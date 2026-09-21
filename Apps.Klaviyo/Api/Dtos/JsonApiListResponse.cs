using Newtonsoft.Json;

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
