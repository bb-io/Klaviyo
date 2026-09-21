using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Helpers;

public static class TranslationValuesHelper
{
    public static string ValidateTargetLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new PluginMisconfigurationException("Target locale is required.");

        return locale.Trim();
    }

    public static string ValidateSourceLocale(string? sourceLocale, string targetLocale, string resourceDisplayName)
    {
        if (string.IsNullOrWhiteSpace(sourceLocale))
            throw new PluginMisconfigurationException(
                $"Source locale is required when translations have not been configured for this {resourceDisplayName.ToLowerInvariant()} yet.");

        var normalized = sourceLocale.Trim();
        if (string.Equals(normalized, targetLocale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        return normalized;
    }

    public static string? ResolveDownloadLocale(
        string? requestedLocale,
        IEnumerable<string> targetLocales,
        string resourceDisplayName,
        string resourceId)
    {
        if (string.IsNullOrWhiteSpace(requestedLocale))
            return null;

        var normalized = requestedLocale.Trim();
        return targetLocales.FirstOrDefault(locale =>
                   string.Equals(locale, normalized, StringComparison.OrdinalIgnoreCase))
               ?? throw new PluginMisconfigurationException(
                   $"{resourceDisplayName} '{resourceId}' does not have locale '{normalized}'.");
    }

    public static IEnumerable<KeyValuePair<string, string>> SelectExportValues(
        IEnumerable<TranslationValueDto> values,
        string? locale) => values.Select(value =>
    {
        var localizedValue = string.IsNullOrWhiteSpace(locale)
            ? null
            : value.Translations.FirstOrDefault(item =>
                string.Equals(item.Key, locale, StringComparison.OrdinalIgnoreCase)).Value;
        return new KeyValuePair<string, string>(value.Id,
            string.IsNullOrWhiteSpace(localizedValue) ? value.SourceValue : localizedValue);
    });

    public static void ValidateValueIds(
        IReadOnlyDictionary<string, string> uploadedValues,
        IReadOnlyCollection<TranslationValueDto> currentValues,
        string resourceDisplayName,
        string resourceId)
    {
        var currentIds = currentValues.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        var unknownIds = uploadedValues.Keys.Where(id => !currentIds.Contains(id)).ToArray();
        if (unknownIds.Length > 0)
            throw new PluginMisconfigurationException(
                $"The content file contains value IDs that do not belong to {resourceDisplayName.ToLowerInvariant()} " +
                $"'{resourceId}': {string.Join(", ", unknownIds)}");
    }

    public static JArray BuildTranslatedValues(
        IReadOnlyDictionary<string, string> uploadedValues,
        string locale) => JArray.FromObject(uploadedValues.Select(value => new
    {
        id = value.Key,
        translations = new Dictionary<string, string> { [locale] = value.Value }
    }));

    public static string[] AddTargetLocale(IEnumerable<string> existingLocales, string newLocale) =>
        existingLocales.Append(newLocale).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
