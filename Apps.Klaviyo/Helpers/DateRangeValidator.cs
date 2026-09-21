using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Klaviyo.Helpers;

public static class DateRangeValidator
{
    public static void Validate(DateTime? updatedFrom, DateTime? updatedTo)
    {
        if (updatedFrom.HasValue && updatedTo.HasValue && updatedFrom > updatedTo)
            throw new PluginMisconfigurationException("'Updated from' cannot be after 'Updated to'.");
    }
}
