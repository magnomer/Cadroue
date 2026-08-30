# Contributing to Cadroue

Cadroue is under active development.

GitHub Issues are currently used primarily as an actionable bug tracker. A short report describing an observable problem is preferable to no report.

## Choose a bug-report form

### Quick bug report

Use the **Quick bug report** when you simply want to report that something went wrong.

GitHub requires an issue title. Give it a short title describing the symptom.

Within the Quick bug report form, two fields are required:

- **What happened?**
- **Cadroue version** — enter `Unknown` only if the version genuinely cannot be determined.

The following information is useful but optional:

- what you were doing when it happened;
- how often it happened;
- other permitted technical information.

Technical diagnosis is not required.

### Detailed bug report

Use the **Detailed bug report** when you already have more information, such as:

- reliable reproduction steps;
- media/container/codec information;
- FFmpeg details;
- sanitized logs or diagnostic output;
- environment details such as GPU/driver, regional settings, storage, networking, display configuration, permissions, or sleep/resume conditions.

The detailed form is optional. A bug is not considered less important because it was reported through the quick form.

## Information policy

Before posting logs, screenshots, attachments, samples, or other report material, read the [Issue Reporting Information Policy](https://github.com/magnomer/Cadroue/blob/main/.github/ISSUE_REPORTING_POLICY.md).

Cadroue may use **permitted technical information about the program and the reported bug** for investigation and development, including source-code fixes, tests, validation, diagnostics, documentation, and AI-assisted development.

For this purpose, permitted technical information must be non-personal, not personally identifiable, not private, not confidential, and not sensitive.

**Personal, personally identifiable, private, confidential, or sensitive information must never be used for bug fixing or related development work, including AI-assisted development.**

Technical relevance does not override this rule.

A reporter may independently provide a sanitized technical description, sanitized diagnostic output, neutralized examples, or synthetic reproduction derived from their own protected material, provided the submitted material itself contains no protected information and does not reveal it.

If a report mixes protected information with independently stated technical information, protected information must be manually excluded and the remaining information reviewed before it is used for debugging, code changes, tests, documentation, or AI-assisted development.

Cadroue must not derive new technical findings by technically analyzing protected information.

Raw issue content must not automatically be sent to AI tools.

A submitted file, log, screenshot, sample, or substantial submitted prose is not automatically reusable project material merely because it contains no protected information. Reuse of the submitted material itself must also be separately permissible.

## Security-related reports

Non-sensitive observable program behavior may be reported through an ordinary bug form even if it may have security implications.

Do not include exploit instructions, working exploit material, credentials, sensitive vulnerability analysis, private attack details, protected information, or other information whose public disclosure would create additional security risk.

A public GitHub Issue is not a private security-reporting channel.

If the repository publishes a private security-reporting method, use that method for sensitive security details. If no private method is published, do not place sensitive vulnerability details in a public issue.

## Reporter responsibilities

Report observable facts.

You do not need to:

- determine the root cause;
- assign severity;
- know which internal subsystem is responsible;
- provide technical media information you do not have.

## Reporter-facing terminology

Issue forms must use user-visible Cadroue terms or ordinary user language.

A reporter must never be required to translate a visible symptom into:

- an internal component or service;
- a code module or class;
- an architectural layer;
- an internal persistence mechanism;
- an internal stream-processing concept;
- another code-oriented classification.

If a problem could belong to several areas, the reporter should choose where they **noticed** it or use **Other / Not sure**. Maintainers perform the internal classification afterward.

## Triage and information handling

Before technical investigation of an issue:

1. check whether the report contains protected information;
2. if protected information is present, do not technically analyze the protected information;
3. handle protected information only as needed for removal, moderation, or requesting a sanitized replacement;
4. separate any independently stated candidate technical information;
5. confirm that the remaining material is permitted technical information;
6. only then use that information for debugging, code, tests, documentation, or AI-assisted development.

Maintainer triage then determines:

- whether the report represents a defect;
- whether it duplicates or overlaps another report;
- the likely underlying area or areas;
- severity;
- current status;
- whether more reproduction evidence is required;
- whether several visible manifestations belong to one underlying defect.

### Reports from older versions

A report from an older Cadroue version may still be useful.

When practical:

1. determine whether the reported behavior is already known to be fixed in a newer version;
2. ask for reproduction on the current version when that would materially clarify whether the defect still exists;
3. if the defect still exists, continue normal triage;
4. if it is no longer reproducible or is known to have been corrected, close it with an appropriate resolution such as `obsolete` or `fixed`, depending on what is known.

A report should not be rejected merely because it came from an older version.

### One issue per underlying defect

Prefer one issue for one underlying defect or root cause.

Several visible symptoms may remain in one issue when investigation shows they originate from the same source defect. A report may also be split if apparently related symptoms are found to have independent causes.

Reporters are not expected to make this determination.

## Labels

### Type

| Label | Meaning |
|---|---|
| `type: bug` | Incorrect or unintended behavior. |

### Severity

Severity describes the consequence of a confirmed defect. It does not describe implementation difficulty and is not assigned by the reporter.

| Label | Use when |
|---|---|
| `severity: critical` | The defect can cause serious data loss or corruption, destructive source modification, or another fundamental failure for which normal use is not reasonably safe. |
| `severity: high` | A major or common workflow is blocked, produces materially wrong or unusable output, or behaves seriously incorrectly without a reasonable workaround. |
| `severity: medium` | Functionality is meaningfully incorrect or impaired, but the scope is limited or a practical workaround exists. |
| `severity: low` | Minor incorrect behavior, cosmetic/UI defect, wording inconsistency, or another low-impact problem that does not materially prevent the intended workflow. |

### Status

| Label | Meaning |
|---|---|
| `status: needs-triage` | Newly reported and not yet classified. |
| `status: needs-reproduction` | More evidence or a reliable reproduction is required. |
| `status: confirmed` | The defect is accepted as real and sufficiently understood to track. |
| `status: in-progress` | An implementation or direct corrective investigation is underway. |
| `status: awaiting-validation` | A corrective change exists, but the issue remains open pending appropriate testing. |
| `status: blocked` | Progress depends on an unresolved prerequisite or external condition. |

Normally keep one status label at a time.

### Area

The reporter-facing **Where did you notice the problem?** field must use only names or concepts that a user can see or reasonably recognize in Cadroue.

Reporter-facing choices:

```text
Files
Sections
Timeline
Split
Edit
Fix
Audio
Convert
Merge
Funnel
Worklist
Viewer / Preview
Export
Relay
Settings
Project
Localization
General
Other / Not sure
```

Reporters must not be required to identify internal architecture, implementation components, persistence mechanisms, stream-processing internals, service names, code modules, or other code-oriented concepts.

The reporter only identifies **where the problem was noticed**. Maintainer triage determines the actual underlying area or code origin.

Maintainer-side labels may be more technical when useful. They are not reporter requirements. Recommended labels may include:

```text
area: files
area: sections
area: timeline
area: split
area: edit
area: fix
area: audio
area: convert
area: merge
area: funnel
area: worklist
area: viewer
area: export
area: relay
area: settings
area: persistence
area: localization
area: cross-cutting
```

## Priority triage for potentially destructive reports

Reports describing possible data loss, destructive source modification, deletion of unrelated user data, project corruption, unintended overwriting, or similarly unsafe behavior must receive priority triage.

Independent reproduction is **not required before the report receives priority attention**.

A credible destructive symptom may be treated as potentially Critical or High during triage even before the defect is confirmed. The reporter does not assign severity; maintainers assess the risk provisionally and revise it as evidence develops.

A typical path is:

```text
credible destructive symptom
        ↓
priority triage
        ↓
provisional risk/severity assessment
        ↓
reproduction and investigation
        ↓
confirmed severity and normal lifecycle
```

This rule prioritizes risk without treating an unconfirmed report as a confirmed defect.

## Resolution and closure

An issue's **status** describes its current state while it is being tracked. A **resolution** describes why the issue is ultimately closed.

Recommended closure outcomes:

| Resolution | Meaning |
|---|---|
| `fixed` | A confirmed defect was corrected and appropriately validated. |
| `duplicate` | Another issue already tracks the same underlying defect or the issue has been consolidated into that report. |
| `not-a-bug` | Investigation determined that the reported behavior is intentional, expected, or otherwise not a defect. |
| `not-reproducible` | The available permitted information is insufficient to reproduce or confirm the reported defect. |
| `obsolete` | The report concerns behavior that no longer exists in relevant current versions or has already been corrected. |

These do not have to be GitHub labels. They may simply be documented as the reason for closure unless the repository later chooses to track them with labels.

When closing an issue, record the reason clearly enough that a reporter can understand what happened.

## Lifecycle

A normal bug may move through:

```text
status: needs-triage
        ↓
status: needs-reproduction   (only when necessary)
        ↓
status: confirmed
        ↓
status: in-progress
        ↓
status: awaiting-validation
        ↓
Closed
```

A commit existing does not automatically mean the defect is verified.

## Current and stable versions

GitHub issue status is separate from Cadroue's `current` and `stable` version states.

- **Current** identifies the current code state that has passed the required minimum tests.
- **Stable** identifies a code state that has passed the complete required tests and applicable real-world validation.

A correction may exist in the current code while its issue remains open under `status: awaiting-validation`.

## Initial repository setup

Tracked files:

```text
.github/
├── CONTRIBUTING.md
├── ISSUE_REPORTING_POLICY.md
└── ISSUE_TEMPLATE/
    ├── bug.yml
    ├── bug-detailed.yml
    └── config.yml
```

Before relying on automatic form labels, create:

```text
type: bug
status: needs-triage
```
