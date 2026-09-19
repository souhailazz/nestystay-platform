# Email and DNS checklist

- [ ] Domain registrar and Cloudflare account are owned by the client.
- [ ] Transactional sender domain is verified in Brevo.
- [ ] Brevo SPF record is published exactly as shown by Brevo.
- [ ] Brevo DKIM record(s) are published and passing.
- [ ] DMARC policy is published (`p=none` during observation, then tighten after review).
- [ ] Human mailbox provider selected: Zoho (recommended) or Google Workspace.
- [ ] Human mailbox SPF/DKIM/DMARC records do not conflict with Brevo.
- [ ] `support@`, `no-reply@`, `security@` and `billing@` ownership is documented.
- [ ] Cloudflare DNS proxy is enabled only for web records; mail records stay DNS-only.
- [ ] TLS certificate issuance is tested after the production A/AAAA record exists.
- [ ] A staging deliverability test and unsubscribe/complaint handling are recorded.

Never place API keys, mailbox passwords or recovery codes in this repository.
