# Gree official support error-code catalog

This directory is a local reference area for official Gree error-code cards.

Source page:
- https://support.gree.com/#/errorCode

Purpose:
- This catalog is only a reference source.
- It is not Telegram bot runtime knowledge.
- Raw cards must not be used directly by the Telegram bot.
- Raw cards must be reviewed and converted before runtime usage.

Processing flow:
Gree official support page -> safe selected download -> raw official card -> text extraction -> engineering review -> approved bot knowledge -> Telegram runtime

Directory layout:
- raw/cards/  : raw official cards and metadata
- review/     : draft reviewed entries
- approved/   : approved entries for later bot knowledge conversion

Safety rules:
- single-threaded requests only
- long delay between requests
- small explicit code/model batches only
- no mass crawling
- no full-site download
- stop immediately on HTTP 403 or 429
- do not connect raw cards directly to runtime

Runtime policy:
- runtimeEnabled: false
- reviewRequiredBeforeBotUsage: true
- massDownloadAllowed: false
