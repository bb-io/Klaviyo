using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Klaviyo.Helpers;

public static class TranslationChannelHelper
{
    public static (string ResourceId, string? Channel) ResolveUploadResourceReference(
        string? inputResourceId,
        IReadOnlyDictionary<string, string> metadata,
        string resourceIdMetadataKey,
        string resourceType,
        IReadOnlyCollection<string> supportedChannels,
        string resourceDisplayName)
    {
        var references = new List<(string ResourceId, string? Channel)>();

        if (!string.IsNullOrWhiteSpace(inputResourceId))
            references.Add(ParseResourceOrTranslationId(
                inputResourceId, resourceType, supportedChannels, resourceDisplayName));

        if (metadata.TryGetValue(resourceIdMetadataKey, out var metadataResourceId) &&
            !string.IsNullOrWhiteSpace(metadataResourceId))
            references.Add(ParseResourceOrTranslationId(
                metadataResourceId, resourceType, supportedChannels, resourceDisplayName));

        if (metadata.TryGetValue("TranslationId", out var metadataTranslationId) &&
            !string.IsNullOrWhiteSpace(metadataTranslationId))
        {
            var parsedTranslation = ParseResourceOrTranslationId(
                metadataTranslationId, resourceType, supportedChannels, resourceDisplayName);
            if (parsedTranslation.Channel is null)
                throw new PluginMisconfigurationException(
                    "The TranslationId file metadata must contain a full translation ID.");
            references.Add(parsedTranslation);
        }

        if (references.Count == 0)
            throw new PluginMisconfigurationException(
                $"{resourceDisplayName} ID is required either as an input or in the HTML metadata.");

        var resourceIds = references
            .Select(reference => reference.ResourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (resourceIds.Length > 1)
            throw new PluginMisconfigurationException(
                $"The content file belongs to {resourceDisplayName.ToLowerInvariant()} '{resourceIds.Last()}', " +
                $"not '{resourceIds.First()}'.");

        metadata.TryGetValue("Channel", out var metadataChannel);
        var channel = ResolveOptionalChannel(
            resourceDisplayName,
            supportedChannels,
            references.Select(reference => reference.Channel).Append(metadataChannel).ToArray());

        return (resourceIds[0], channel);
    }

    public static string? GetChannelFromTranslationId(string translationId)
    {
        var parts = translationId.Split("::", StringSplitOptions.None);
        return parts.Length == 3 ? parts[1] : null;
    }

    public static (string ResourceId, string? Channel) ParseResourceOrTranslationId(
        string? value,
        string resourceType,
        IReadOnlyCollection<string> supportedChannels,
        string resourceDisplayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException($"{resourceDisplayName} ID is required.");

        var normalized = value.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return (normalized, null);

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length != 3 ||
            !string.Equals(parts[0], resourceType, StringComparison.OrdinalIgnoreCase) ||
            !supportedChannels.Contains(parts[1], StringComparer.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parts[2]))
            throw new PluginMisconfigurationException(
                $"Expected a {resourceDisplayName.ToLowerInvariant()} ID or a {resourceType} translation ID " +
                $"for one of these channels: {string.Join(", ", supportedChannels)}.");

        return (parts[2], parts[1].ToLowerInvariant());
    }

    public static string? NormalizeOptionalChannel(
        string? channel,
        IReadOnlyCollection<string> supportedChannels,
        string resourceDisplayName)
    {
        if (string.IsNullOrWhiteSpace(channel))
            return null;

        var normalized = channel.Trim().ToLowerInvariant();
        if (!supportedChannels.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Channel '{channel}' is not supported for {resourceDisplayName.ToLowerInvariant()}. " +
                $"Supported channels: {string.Join(", ", supportedChannels)}.");

        return normalized;
    }

    public static string ResolveChannel(
        string resourceDisplayName,
        IReadOnlyCollection<string> supportedChannels,
        string? fallbackChannel,
        params string?[] candidates)
    {
        var resolved = ResolveOptionalChannel(resourceDisplayName, supportedChannels, candidates);
        if (resolved is not null)
            return resolved;

        var fallback = NormalizeOptionalChannel(fallbackChannel, supportedChannels, resourceDisplayName);
        return fallback ?? throw new PluginMisconfigurationException(
            $"Channel is required for {resourceDisplayName.ToLowerInvariant()}.");
    }

    public static string? ResolveOptionalChannel(
        string resourceDisplayName,
        IReadOnlyCollection<string> supportedChannels,
        params string?[] candidates)
    {
        var normalized = candidates
            .Select(channel => NormalizeOptionalChannel(channel, supportedChannels, resourceDisplayName))
            .Where(channel => channel is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length > 1)
            throw new PluginMisconfigurationException(
                $"Conflicting channels were provided for {resourceDisplayName.ToLowerInvariant()}: " +
                $"{string.Join(", ", normalized)}.");

        if (normalized.Length == 1)
            return normalized[0];

        return null;
    }
}
