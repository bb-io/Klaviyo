# Blackbird.io Klaviyo

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Documentation coming soon.

### Template translations

Use **Download template** to export a template for a target locale as XLIFF and JSON. Translate the XLIFF, then pass it to **Upload template** with the same target locale. If that locale does not exist yet, the upload action adds it while preserving the template's existing locales. Set **Source locale** when translations have not been enabled for the template yet.

Template XLIFF files are generated and read through Blackbird Filters. Upload accepts supported XLIFF 1.x and 2.x files and preserves inline HTML tags. The JSON file is reference data; the upload action takes the translated XLIFF file.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
