using Apps.Klaviyo.Services;

namespace Tests.Klaviyo;

[TestClass]
public class TemplateFileServiceTests
{
    [TestMethod]
    public void Adding_a_new_locale_preserves_existing_locales()
    {
        var locales = TemplateFileService.AddTargetLocale(["de", "fr"], "it");

        CollectionAssert.AreEqual(new[] { "de", "fr", "it" }, locales);
        CollectionAssert.AreEqual(new[] { "de", "fr" },
            TemplateFileService.AddTargetLocale(["de", "fr"], "FR"));
    }
}
