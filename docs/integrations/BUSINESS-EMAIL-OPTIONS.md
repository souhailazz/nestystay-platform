# Business mailbox options

## Recommended: Zoho Mail

Use a client-owned domain mailbox such as `support@client-domain`, `no-reply@client-domain` and `security@client-domain`. Zoho is the low-cost default for human correspondence; Brevo remains the transactional sender. Configure SPF, DKIM and DMARC in the client DNS and use a unique admin account with MFA.

## Alternative: Google Workspace

Choose Google Workspace when the client already standardizes on Google identity, Drive and Calendar. Keep the same mailbox names and DNS controls. Workspace is not required for application delivery; it is a business mailbox choice.

## Not used

Alibaba Mail and Alibaba SMTP are not application dependencies and are not part of the recommended operating plan. Alibaba Cloud eKYC is unrelated and remains preserved.

## Client decision

Select Zoho or Google Workspace, provide the domain owner, mailbox names, recovery contacts and an MFA-protected administrator. Do not send mailbox passwords through source control or issue comments.
