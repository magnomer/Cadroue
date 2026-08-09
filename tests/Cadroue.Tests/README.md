# Cadroue.Tests

Test layout and policy for the behaviour-focused test suite.

## Interface boundary

Every production operation used by a test is relayed through a `T`-prefixed adapter or helper under `/Interface`. This includes pure value operations and production object construction; test bodies do not invoke methods or constructors from `src` directly.

An adapter may translate, invoke, observe, and clean up. It must not repair the behaviour under test: each operation transparently delegates to the production path, never reimplements production logic, fakes notifications, or returns success the production path did not produce.

`InterfaceBoundaryTests` enforces this boundary for future test changes.

## Naming rule

Name test files and classes after behaviour, not production type. Name test methods after observable behaviour (`SectionPastMediaDuration_IsRejected`), not the production method that ran.
