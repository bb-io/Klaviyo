# Blackbird.io Klaviyo

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Klaviyo is a marketing automation platform that helps brands deliver personalized customer experiences across email, SMS, mobile push, and web.

## Before setting up

Create a private API key under **Settings > API keys** and grant it the following scopes:

- `translations:read`
- `translations:write`
- `campaigns:read`

For more information, see Klaviyo's [private API key guide](https://help.klaviyo.com/hc/en-us/articles/7423954176283).

## Connecting

1. Navigate to **Apps** and search for Klaviyo.
2. Select **Add Connection**.
3. Enter a name for the connection.
4. Enter your Klaviyo private API key.
5. Select **Connect**.

## Actions

### Templates

- **Search templates** returns templates with configured translations and supports filtering by channel and update date.
- **Get template** returns the selected template.
- **Download template** downloads the source or a selected translation as HTML and JSON files.
- **Upload template** uploads translated HTML to an existing or new locale.

### Campaign variations

- **Search campaign variations** returns campaign variations with configured translations and supports filtering by channel and update date.
- **Get campaign variation** returns the selected campaign variation.
- **Download campaign variation** downloads the source or a selected translation as HTML and JSON files.
- **Upload campaign variation** uploads translated HTML to an existing or new locale.

### Flow messages

- **Search flow messages** returns flow messages with configured translations and supports filtering by channel and update date.
- **Get flow message** returns the selected flow message associated with a translation.
- **Download flow message** downloads the source or a selected translation as HTML and JSON files.
- **Upload flow message** uploads translated HTML to an existing or new locale.

### Universal content

- **Search universal content** returns universal content with configured translations and supports filtering by channel and update date.
- **Get universal content** returns the selected universal content.
- **Download universal content** downloads the source or a selected translation as HTML and JSON files.
- **Upload universal content** uploads translated HTML to an existing or new locale.

### Content

- **Search content** returns all supported content and supports filtering by content type, channel, and update date.
- **Download content** downloads the selected content as HTML and JSON files.
- **Upload content** uploads translated HTML to the selected content type and locale.

## Events

- **On content added or updated** triggers when supported Klaviyo content is added or updated. The event can be filtered by content type.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
