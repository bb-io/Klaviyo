using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Klaviyo.Helpers;

public static class ExceptionHelper
{
    public static PluginMisconfigurationException UnsupportedContentType() =>
        new("Unsupported content type. Select a value from the available options.");
}
