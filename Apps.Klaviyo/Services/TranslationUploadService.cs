using Apps.Klaviyo.Api;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Klaviyo.Services;

public static class TranslationUploadService
{
    public static async Task UploadAsync(
        KlaviyoClient client,
        TranslationUploadContext context)
    {
        var existing = await TranslationResourceHelper.FindTranslationAsync(
            client,
            context.ResourceType,
            context.ResourceId,
            context.SupportedChannels,
            context.Channel);

        var locale = existing?.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, context.Locale, StringComparison.OrdinalIgnoreCase))
                     ?? context.Locale;

        var translation = existing is null
            ? await TranslationResourceHelper.CreateTranslationAsync(
                client,
                context.ResourceType,
                context.ResourceDisplayName,
                context.ResourceId,
                locale,
                context.SourceLocale,
                context.Channel)
            : existing;

        if (existing is not null &&
            string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        if (context.UpdateTargetLocalesBeforeValues &&
            !translation.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase))
        {
            var updateTargetLocalesRequest = TranslationRequestBuilder.UpdateTargetLocales(
                translation.Id,
                TranslationValuesHelper.AddTargetLocale(translation.Attributes.TargetLocales, locale));
            await client.ExecuteWithErrorHandling(updateTargetLocalesRequest);
        }

        var current = await TranslationResourceHelper.GetWithValuesAsync(client, translation.Id);
        TranslationValuesHelper.ValidateValueIds(
            context.Values,
            current.Attributes.Values,
            context.ResourceDisplayName,
            context.ResourceId);

        var targetLocales = context.UpdateTargetLocalesBeforeValues ||
                            current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase)
            ? null
            : TranslationValuesHelper.AddTargetLocale(current.Attributes.TargetLocales, locale);
        var updateValuesRequest = TranslationRequestBuilder.UpdateValues(
            current.Id,
            context.Values,
            locale,
            targetLocales);
        await client.ExecuteWithErrorHandling(updateValuesRequest);
    }
}

public record TranslationUploadContext(
    string ResourceType,
    string ResourceDisplayName,
    string ResourceId,
    IReadOnlyCollection<string> SupportedChannels,
    string Channel,
    string Locale,
    string? SourceLocale,
    IReadOnlyDictionary<string, string> Values,
    bool UpdateTargetLocalesBeforeValues = false);
