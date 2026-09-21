using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Helpers;

public static class TranslationSearchHelper
{
    public static TranslationResponse MapTranslation(
        TranslationDto translation,
        IReadOnlyDictionary<string, RelatedResourceDto> included)
    {
        var relationship = translation.Relationships.Properties()
            .Select(property => new
            {
                ResourceType = property.Name,
                ResourceId = property.Value["data"]?["id"]?.ToString()
            })
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.ResourceId));

        var resourceType = relationship?.ResourceType ?? GetResourceTypeFromId(translation.Id);
        var resourceId = relationship?.ResourceId ?? GetResourceIdFromId(translation.Id);
        included.TryGetValue($"{resourceType}::{resourceId}", out var relatedResource);

        return new TranslationResponse
        {
            ContentId = translation.Id,
            ResourceId = resourceId,
            ResourceType = resourceType,
            Name = GetResourceName(relatedResource?.Attributes),
            Channel = translation.Attributes.Channel,
            SourceLocale = translation.Attributes.SourceLocale,
            TargetLocales = translation.Attributes.TargetLocales,
            FallbackLocale = translation.Attributes.FallbackLocale,
            Created = translation.Attributes.Created?.UtcDateTime,
            Updated = translation.Attributes.Updated?.UtcDateTime
        };
    }

    public static bool Matches(
        TranslationResponse item,
        IReadOnlyCollection<string> resourceTypes,
        IReadOnlyCollection<string> channels,
        SearchTranslationsRequest input)
    {
        if (resourceTypes.Count > 0 && !resourceTypes.Contains(item.ResourceType, StringComparer.OrdinalIgnoreCase))
            return false;

        if (channels.Count > 0 && (item.Channel is null ||
                                  !channels.Contains(item.Channel, StringComparer.OrdinalIgnoreCase)))
            return false;

        if (input.UpdatedFrom.HasValue && (!item.Updated.HasValue || item.Updated < input.UpdatedFrom))
            return false;

        if (input.UpdatedTo.HasValue && (!item.Updated.HasValue || item.Updated > input.UpdatedTo))
            return false;

        return true;
    }

    public static List<string> NormalizeAndValidate(
        IEnumerable<string>? values,
        IEnumerable<string> supportedValues,
        string valueName)
    {
        var supported = supportedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var unsupported = normalized.Where(value => !supported.Contains(value)).ToArray();
        if (unsupported.Length > 0)
            throw new PluginMisconfigurationException(
                $"Unsupported {valueName} value(s): {string.Join(", ", unsupported)}.");

        return normalized;
    }

    private static string GetResourceTypeFromId(string translationId) =>
        translationId.Split("::", StringSplitOptions.None).FirstOrDefault() ?? string.Empty;

    private static string GetResourceIdFromId(string translationId) =>
        translationId.Split("::", StringSplitOptions.None).LastOrDefault() ?? string.Empty;

    private static string? GetResourceName(JObject? attributes) =>
        attributes?["name"]?.ToString()
        ?? attributes?.SelectToken("definition.name")?.ToString();
}
