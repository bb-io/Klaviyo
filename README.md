# Blackbird.io Klaviyo

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Documentation coming soon.

### Template translations

Use **Download template** without a locale to export the source template as HTML and JSON. Provide the optional locale to download an existing localization. Translate the HTML, then pass it to **Upload template** with the target locale. If that locale does not exist yet, the upload action adds it while preserving the template's existing locales. The Template ID input is optional on upload because it is read from the downloaded HTML metadata. Set **Source locale** when translations have not been enabled for the template yet.

Template HTML is created through Blackbird Filters so translation actions can protect and restore its inline tags. The file stores the template ID in `blackbird-TemplateId` metadata and wraps every Klaviyo translation value in an element with a `data-klaviyo-translation-key` attribute. This allows the upload action to map every translated element back to its Klaviyo value ID without assuming that a template only contains a body value. The JSON file is reference data; the upload action takes the translated HTML file.

### Campaign variation translations

Use **Download campaign variation** without a locale to export all source values as HTML and JSON, or select an existing locale to export its translations. Campaign fields and editor blocks such as subject, preview text, sender name, text content, links, and alt text are exported as separate elements identified by their Klaviyo value IDs.

The HTML stores the variation ID in `blackbird-CampaignVariationId` metadata and uses `data-klaviyo-translation-key` attributes to preserve the mapping. The Campaign variation ID input is optional on upload because it is read from that metadata. **Upload campaign variation** accepts an existing or new target locale. When adding a locale, it preserves all existing target locales and updates the translated values in the same request. **Source locale** is only required when translations have not yet been configured for the variation.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
