# M365Webhooks
M365Webhooks is a crossplatform .NET 6 console application designed to consume Microsoft API data and relay it as JSON webhooks. This is especially useful for SOCs that are not using Splunk or any of the SEIMs with an official method to retreive data from Microsoft APIs.

For Elasticsearch, this is a replacement for the Microsoft filebeat plugin, the filebeat pluggin only allows retrieval of data from a single Microsoft 365 tenant. This application was born to work around that limitation and retreive data across any number of Microsoft 365 tenants.

The binary (M365Webhooks.dll) requires that you have the .NET 6.0 runtime installed.

**Current Release: v1.0.3 (Release Candidate)**

Instructions Here: https://github.com/White-Knight-IT/M365Webhooks/wiki

**Current Microsoft APIs:**

Microsoft Threat Protection (https://api.security.microsoft.com)

Office 365 Management API (https://manage.office.com)
