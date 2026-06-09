# Equipment Diagnostics Contract Examples

These files are deterministic documentation examples for API, bot, formatter, and staging-validation consumers.

They are part of the ED-10 manual-ingestion and bot-readiness pack. Their job is to document stable DTO and formatter shapes for future clients, not to provide diagnostic source material.

They are not runtime knowledge:

- They must not be embedded into `AssistantEngineer.Modules.EquipmentDiagnostics`.
- They must not be loaded by `EquipmentDiagnosticsJsonKnowledgeSource`.
- They must not be treated as source of truth for diagnostic content.
- They must not change runtime catalog counts, search results, or catalog index facets.

Rules for examples:

- Keep examples small and focused on contract shape.
- Use existing runtime seed entries such as Gree GMV H5, Gree Chiller E6, or Gree Indoor H6.
- Keep unverified seed examples visibly low confidence and verification-required.
- Do not invent manual titles, document codes, pages, sections, quotes, or manual evidence.
- Do not include long copyrighted manual text.
- Do not include protection-defeat or safeguard-deactivation instructions.
- Future Telegram/UI clients should consume the runtime DTO fields or deterministic formatter output instead of generating their own diagnostic prose.
- Bot response examples document `Answer`, `ClarificationRequired`, `ReferenceOnly`, and `NotFound` shapes from the runtime-only ED-15B endpoint.
- Bot examples must never present staging candidates, manual codebook occurrences, generated previews, or local manuals as final diagnostic answers.
- ED-15C contract tests require explicit manufacturer/code in the request and validate status, message/title, verification, confidence, safety, source where relevant, clarification options, and safe next steps.
- Examples must remain within endpoint request limits and must not expose internal artifact or filesystem paths.
