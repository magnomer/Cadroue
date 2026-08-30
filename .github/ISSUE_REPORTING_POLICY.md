# Issue Reporting Information Policy

This policy governs how information submitted or gathered through Cadroue GitHub issue reports may be handled and used for Cadroue bug investigation and development.

## 1. Scope

This policy applies to information associated with a Cadroue issue report, including:

- issue-form answers;
- issue titles and descriptions;
- comments and follow-up replies;
- edits to issue content;
- screenshots;
- logs and diagnostic output;
- attachments;
- linked samples or files intentionally supplied for the report;
- technical information supplied later in response to maintainer questions;
- technical findings produced during investigation.

This policy applies to Cadroue maintainers, contributors, reviewers, and anyone acting on behalf of or contributing development work to Cadroue.

This policy governs **Cadroue's use of report information for bug fixing and related project engineering**.

GitHub may separately process or display account, profile, activity, timestamp, platform, or other information under GitHub's own services and policies. Cadroue does not control that platform processing.

A reporter's GitHub identity or other platform-associated personal information is protected information under this policy and must not be used for Cadroue bug fixing.

## 2. Reporter burden

The information categories in this policy govern how Cadroue handles report information. They do not require reporters to understand internal code structure or classify the technical cause of a bug.

Reporters should describe what happened using the Cadroue terms they can see or ordinary language. Maintainers are responsible for translating that report into internal technical classifications.

## 3. Information categories

Information connected with an issue report is treated in two mutually exclusive categories for development use.

### Permitted technical information

Permitted technical information is information about Cadroue or the reported defect that is:

- non-personal;
- not personally identifiable;
- not private;
- not confidential;
- not sensitive.

Examples may include:

- observable program behavior;
- reproduction conditions that contain no protected information;
- Cadroue settings and processing choices;
- error codes and non-protected error messages;
- properly sanitized diagnostic output;
- non-protected media container, codec, stream, timing, framing, or corruption characteristics;
- operating conditions technically relevant to the defect;
- technical relationships between reports;
- technical findings produced without Cadroue-side prohibited use of protected information.

### Protected information

For this policy, **protected information** means any information that is personal, personally identifiable, private, confidential, or sensitive.

Examples may include:

- real names;
- account names or user names;
- email addresses;
- addresses or contact information;
- identifiers associated with a person;
- private filenames or paths;
- confidential project, client, employer, or organization names;
- private network locations;
- credentials or authentication information;
- private communications;
- private documents or media;
- confidential or sensitive technical specifications;
- proprietary or non-public technical information;
- sensitive metadata;
- other information not appropriate for use in public software development.

### Precedence rule

**Protected status always takes precedence over technical relevance.**

Information does not become permitted merely because it is useful, technical, necessary to reproduce a bug, or directly related to Cadroue.

If information is both technically relevant and personal, personally identifiable, private, confidential, or sensitive, it is protected information and must not be used for bug fixing.

## 4. Permitted use of technical information

Permitted technical information supplied in a report or gathered while investigating it may be used for Cadroue development and maintenance.

It may be used to:

- reproduce and investigate reported behavior;
- determine whether the behavior is a defect;
- identify an underlying technical cause;
- compare, relate, or combine reports sharing a technical cause;
- modify Cadroue source code;
- design and implement corrections;
- create or revise automated tests;
- create synthetic or neutral test cases and samples;
- validate corrections;
- prevent regressions;
- improve diagnostics and error reporting;
- improve documentation;
- improve related program behavior or architecture;
- support AI-assisted development for those technical purposes.

A reporter should therefore assume that **permitted technical information concerning Cadroue or a reported bug may be incorporated into Cadroue engineering work, including source-code fixes, tests, diagnostics, and documentation**.

Technical information from one report may also be used to correct other Cadroue defects when the same or a related technical cause is involved.

## 5. Purpose limitation

Permitted technical information gathered through issue reports is intended for Cadroue development, maintenance, testing, validation, documentation, diagnostics, and directly related project engineering.

It must not be repurposed by the Cadroue project for unrelated activities such as:

- advertising;
- marketing profiling;
- personal profiling;
- selling reporter information;
- building unrelated personal-information datasets;
- other purposes unrelated to Cadroue engineering.

## 6. Protected information must never be used for bug fixing

**Personal, personally identifiable, private, confidential, or sensitive information must never be used for Cadroue bug fixing or related development work.**

This prohibition includes use in:

- debugging and technical investigation;
- defect reproduction;
- source-code changes;
- test design or execution;
- test fixtures or samples;
- diagnostic development;
- documentation or examples;
- code review;
- development-oriented issue summarization;
- AI-assisted analysis, debugging, coding, testing, summarization, review, or documentation.

The fact that protected information appears in a public issue does not make it usable for development.

Contributions, patches, tests, documentation, or other project material derived through Cadroue-side prohibited use of protected information must not be accepted into Cadroue.

## 7. Reporter-supplied sanitized technical information

The prohibition on using protected information does **not** prevent a reporter from independently preparing and submitting sanitized technical information about a problem that originally occurred with protected material.

For example, a reporter may independently provide:

- a sanitized technical description;
- non-protected codec or container characteristics;
- sanitized diagnostic output;
- a neutralized path example;
- a synthetic reproduction;
- other technical facts that, as submitted, contain no protected information.

Such reporter-supplied information may be used if:

- the submitted information itself is permitted technical information;
- it does not reveal or reasonably permit reconstruction of protected information;
- Cadroue does not need to inspect or use the underlying protected material in order to use the submitted technical information.

Cadroue does not need to reject a permitted technical fact merely because the reporter learned that fact from their own protected material before submitting it.

## 8. Separable information in a mixed report

A report may contain both protected information and technical information that already exists independently and remains meaningful without the protected information.

For example:

```text
FFmpeg exited with code -22.
Source: C:\ConfidentialClient\ProjectX\private-video.mp4
```

The private path is protected information.

The independently stated fact that FFmpeg exited with code `-22` may remain usable after manual separation if it is otherwise permitted technical information.

The presence of protected information elsewhere in the same issue does not automatically make every other statement unusable.

However, Cadroue must not obtain new technical facts by examining the protected portion of the report or a protected attachment.

## 9. No Cadroue-side derived-use loophole

Cadroue maintainers, contributors, reviewers, automated systems, and AI tools must not derive technical information for bug fixing by examining, analyzing, decoding, processing, summarizing, or otherwise technically using protected information.

A technical conclusion is not permitted merely because the conclusion itself contains no protected information if Cadroue obtained that conclusion through prohibited technical use of protected information.

For example, if a private media file is protected information, Cadroue must not inspect that file and then retain only a technical conclusion such as its timestamp pattern, stream structure, or corruption characteristics.

Instead, the reporter should provide permitted technical information, sanitized output, or a synthetic reproduction that can be used without Cadroue consulting the protected file.

## 10. Limited handling of protected information

The prohibition on bug-fixing use does not prevent the minimal handling necessary to protect the information itself.

Protected information may be handled only as necessary to:

- recognize that protected information is present;
- redact or remove it;
- moderate the affected issue or attachment;
- request a sanitized replacement;
- respond to an accidental disclosure.

Such handling is **not bug-fixing use** and must not expand into technical investigation of the defect.

Protected information must not be supplied to AI tools for identification, sanitization, redaction, moderation, summarization, or any other Cadroue development purpose.

Sanitization and separation must occur **before** any resulting report-derived material is used with AI-assisted development tools.

Any project-controlled temporary copy of protected information created solely for removal, moderation, or redaction must be deleted when that handling is complete, where deletion is reasonably under the project's control.

## 11. Separation rule

When a report contains both potentially useful technical information and protected information, the two must be separated before any technical information enters bug-fixing work.

The required flow is:

```text
mixed report material
        ↓
manual identification and separation
        ↓
protected information → excluded from bug-fixing work
independently stated candidate technical information → reviewed for permissibility
        ↓
only permitted technical information
        ↓
debugging / code / tests / diagnostics / documentation / AI
```

Protected information must never be passed into development tools merely for those tools to remove, classify, summarize, or sanitize it.

A sanitized result is usable only if:

- no protected information remains;
- the remaining information does not reasonably reveal or reconstruct the removed protected information;
- Cadroue did not obtain the remaining technical facts through prohibited technical use of protected information.

If those conditions cannot be met, the report material must not be used for bug fixing.

## 12. Substitutes and reproductions

If a technical problem cannot be investigated without relying on protected information, maintainers should request a sanitized description, sanitized log, synthetic reproduction, or other non-protected substitute.

A maintainer-created reproduction may be used only when it can be constructed from already-permitted information without consulting, analyzing, or depending on protected information.

If no permissible reproduction or technical description is available, Cadroue must not investigate the defect on the basis of the protected information. The issue may remain unconfirmed or unresolved until permissible information becomes available.

## 13. Logs and diagnostic output

Logs and diagnostic output can contain useful technical information together with protected information, including:

- names or account names;
- local paths;
- identifying or confidential filenames;
- network locations;
- private project or organization names;
- device or system identifiers;
- media metadata;
- command-line arguments containing private values.

Reporters should review and redact such material before posting it.

If a submitted log contains protected information, Cadroue must not technically analyze the protected portions. Independently stated or manually separable permitted technical information may still be used after separation.

Where separation is insufficient or uncertain, a sanitized replacement should be requested.

## 14. Screenshots

Screenshots may contain protected information unrelated to the defect.

Before posting a screenshot, reporters should check for:

- private filenames or paths;
- user names or account information;
- unrelated applications or windows;
- private media content;
- confidential project information;
- other sensitive material.

If a submitted screenshot contains protected information, Cadroue must not technically analyze the protected portions. Maintainers may request a cropped, redacted, or otherwise sanitized replacement.

## 15. Submitted artifacts, prose, and technical facts are different

Permission to use permitted technical facts does not automatically make submitted material reusable project content.

This distinction applies to:

- files;
- screenshots;
- logs;
- media samples;
- issue prose;
- comments;
- other expressive or authored material.

A technical fact may be usable for investigation while the submitted artifact or wording itself may still be subject to copyright, ownership, licensing, contractual, confidentiality, or other restrictions.

A submitted artifact or substantial submitted prose must not be copied into Cadroue source, test fixtures, samples, documentation, or distributions unless that reuse is separately permissible.

Where practical, Cadroue development should restate technical facts neutrally and reproduce relevant conditions using synthetic or otherwise clearly reusable material.

## 16. Media samples and other files

Do not submit private, confidential, personally sensitive, proprietary, or otherwise restricted media or files for bug fixing.

Prefer:

1. a synthetic reproduction file;
2. a freely distributable sample;
3. a properly sanitized or reduced sample containing only permitted technical characteristics necessary to reproduce the defect.

Only files appropriate for public technical investigation should be submitted.

If a defect originally occurs with a protected file, the reporter may provide permitted technical characteristics or an independently created sanitized or synthetic reproduction instead.

## 17. Derived development material

Permitted technical information may be transformed into development material such as:

- source-code corrections;
- regression tests;
- synthetic malformed-media samples;
- validation rules;
- diagnostic rules;
- updated error messages;
- documentation of technical behavior.

Derived development material must not contain, depend on, reproduce, or have been obtained through Cadroue-side prohibited use of protected information.

It must also respect any independent rights or restrictions affecting submitted artifacts or prose.

## 18. AI-assisted development

Cadroue development may use AI-assisted development tools.

Only permitted technical information may be used with those tools.

**Protected information from an issue report must never be supplied to or used with AI tools for Cadroue bug fixing or related development work.**

This includes using an AI tool merely to redact, sanitize, classify, inspect, or summarize protected issue content.

Raw issue content must not automatically be submitted to AI tools.

Before report-derived information is used with AI tools:

1. the report must be checked for protected information;
2. if protected information is present, technical analysis of the protected information must not begin;
3. protected information must be manually excluded from the material intended for development use;
4. the remaining material must be reviewed to confirm that it is permitted technical information;
5. only then may that permitted information enter AI-assisted development.

## 19. Public nature of GitHub Issues

Cadroue GitHub Issues are public.

Information intentionally posted to an issue should be treated as publicly visible. Closing an issue does not make previously posted information private.

Public visibility does not change Cadroue's development-use rules: protected information must never be used for bug fixing.

Reporters should avoid posting protected information and provide only information reasonably needed to describe the problem.

## 20. Security-related reports

Non-sensitive observable program behavior may be reported through the ordinary public bug forms even when the reporter believes it could have security implications.

For example, a reporter may describe a harmless observable symptom without including sensitive exploitation details.

Do **not** publish through a public issue:

- exploit instructions;
- working exploit material;
- secrets or credentials;
- sensitive vulnerability analysis;
- private attack details;
- protected information;
- other details whose public disclosure would create additional security risk.

A public bug form is not a private security-reporting channel.

If the repository publishes a private security-reporting method, use that method for sensitive security details. If no such method is published, do not place sensitive vulnerability details in a public issue.

## 21. Closing, editing, or deleting an issue

Closing, editing, or deleting an issue does not require Cadroue to remove permitted technical information that has already been legitimately incorporated into source code, tests, diagnostics, documentation, or other project engineering under this policy.

Protected information must never have been incorporated into those materials.

If protected information is later discovered in development material, it should be removed or replaced rather than retained because the originating issue was public or has since been closed.

## 22. Accidental disclosure

If protected information is posted accidentally, it should be removed or redacted as soon as reasonably possible.

It must not be carried forward into source code, tests, samples, documentation, AI-assisted development, or other bug-fixing material.

Independently stated or manually separable permitted technical information may still be used after separation. Cadroue must not derive additional technical facts by analyzing the protected information itself.

If no permissible information is available, maintainers should request a sanitized or synthetic replacement.

Because publicly posted information may already have been distributed through notifications, caches, mirrors, or other systems, removal from the original issue cannot guarantee complete retraction from every external copy.
