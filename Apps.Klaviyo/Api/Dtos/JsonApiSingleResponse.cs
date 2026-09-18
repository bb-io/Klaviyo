using Newtonsoft.Json;

namespace Apps.Klaviyo.Api.Dtos;

public class JsonApiSingleResponse<T>
{
    [JsonProperty("data")]
    public T Data { get; set; } = default!;
}
