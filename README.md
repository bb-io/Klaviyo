# Blackbird.io Klaviyo

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Documentation coming soon.

### Template translations

Use **Download template** to export the source template or a specific locale as HTML and JSON. Translate the HTML, then pass it to **Upload template** with the target locale. If that locale does not exist yet, the upload action adds it while preserving the template's existing locales. Set **Source locale** when enabling translations for a template for the first time.

For templates with multiple translation values, the exported HTML contains a separate `data-klaviyo-value-id` block for each value. Keep those IDs and the HTML structure of translated blocks intact so the upload action can map each translation back to Klaviyo. The JSON file is reference data; the upload action takes the HTML file.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
