# Security research report

## Metadata

- Report name: `Syrx.Oracle-security-research-report-20260322.md`
- Generated on: `2026-03-22`
- Assessor: `security-researcher`
- Scope: `Syrx.Oracle.sln`, including first-party Oracle packages, referenced Syrx submodule projects, GitHub workflows, and the updated Oracle integration-test fixture on branch `3.0.0`
- Report location: `/.docs/research/security/`
- Evidence sources: source inspection, workflow inspection, workspace search results, `dotnet list Syrx.Oracle.sln package --vulnerable --include-transitive`, targeted integration-test execution, workspace instructions, and authoritative platform references

## Scope and constraints

- In scope:
  - All projects included by `Syrx.Oracle.sln`
  - First-party Oracle connector, extension, and dynamic-parameter packages
  - Referenced `Syrx.Commanders.Databases` and nested `Syrx` submodule projects that participate in the solution build
  - `.github/workflows` and `.github/dependabot.yml`
  - Oracle integration-test infrastructure, including `OracleFixture`
  - Direct and transitive NuGet dependency vulnerability posture
- Out of scope:
  - GitHub repository settings, environments, required-reviewer rules, and release permissions not represented in source control
  - NuGet.org publisher settings and external infrastructure controls
  - Container-registry-side malware scanning or provenance data not available locally
- Constraints:
  - No `orchestrator` tool was available in the current toolset, so the assessment was executed directly against the workspace.
  - CodeQL and Gitleaks artifacts were not available locally; only workflow definitions were reviewed.
  - The targeted integration-test invocation produced truncated terminal output after building through the Oracle projects, so final pass/fail summary visibility was limited by tooling. The earlier compile blocker previously tied to `OracleBuilder()` was not reproduced in this run.

## Methodology

- Workspace instructions reviewed:
  - `.github/instructions/security-and-secure-coding.instructions.md`
  - `.github/instructions/documentation-and-specifications.instructions.md`
  - `.github/instructions/github-actions-ci-cd-best-practices.instructions.md`
  - `.github/instructions/csharp-development-and-standards.instructions.md`
- Skills used:
  - `security-research`
  - `critical-thinking`
  - `api-design`
  - `syrx-data-access`
- Agents consulted through orchestrator:
  - None. `orchestrator` was not available in the current toolset.
- Research methods:
  - Reviewed the current `3.0.0` branch state and inspected the final working tree.
  - Re-read the current `OracleFixture` implementation to verify whether the earlier sensitive-logging finding still applied.
  - Inspected `publish.yml`, `security.yml`, and `dependabot.yml` for least-privilege, deployment protection, and supply-chain controls.
  - Searched for SQL interpolation, sensitive logging, secret-handling patterns, and mutable external dependency usage.
  - Reviewed JSON and XML settings `UseFile` extensions for path-validation controls.
  - Executed `dotnet list Syrx.Oracle.sln package --vulnerable --include-transitive`.
  - Executed a targeted `dotnet test` run for `tests/integration/Syrx.Oracle.Tests.Integration` to confirm whether the prior integration-test compile issue remained.

## Executive summary

The final working tree on branch `3.0.0` is materially better than the earlier pre-fix snapshot. The previously reported connection-string logging issue in `OracleFixture` is no longer present in the current code, and the earlier integration-test compile blocker tied to the fixture changes was not reproduced during this assessment. The NuGet vulnerability audit also returned no known vulnerable direct or transitive packages from the configured sources.

The remaining confirmed risks are limited and are concentrated in CI/CD and test-infrastructure supply-chain posture rather than in the production Oracle connector code. Two findings remain supported by current evidence:

1. The NuGet publication workflow still publishes from a release event without a source-visible GitHub environment approval gate.
2. The Oracle integration test container image is still pinned by mutable tag rather than immutable digest.

No confirmed SQL injection, path traversal, or secret hardcoding was identified in the reviewed production code paths. The Syrx JSON and XML `UseFile` extensions continue to enforce leaf-filename plus extension allow-lists, which is a positive control against path traversal in those registration APIs.

## Findings summary

| ID | Severity | Confidence | Category | Affected area | Short title |
|---|---|---|---|---|---|
| SEC-001 | Medium | High | CI/CD / Access Control | `.github/workflows/publish.yml` deploy job | NuGet publish path lacks environment approval gate |
| SEC-002 | Low | High | Supply Chain Integrity | `tests/integration/Syrx.Oracle.Tests.Integration/OracleFixture.cs` | Oracle test container image is pinned only by mutable tag |

## Detailed findings

### SEC-001 `NuGet publish path lacks environment approval gate`

- Severity: `Medium`
- Confidence: `High`
- Category: `CI/CD / Access Control`
- CWE: `CWE-284 Improper Access Control`
- OWASP: `OWASP Top 10 2021 A01: Broken Access Control`
- Affected files or symbols:
  - `.github/workflows/publish.yml`
  - `deploy` job
- Evidence:
  - The workflow publishes packages when `github.event_name == 'release'` and invokes `dotnet nuget push ... --api-key "${{ secrets.NUGET_API_KEY }}"`.
  - The `deploy` job in source has no `environment:` declaration, so there is no source-visible deployment protection boundary or required-reviewer gate tied to the publish step.
  - Workspace CI/CD guidance explicitly calls for protecting production with environments and approvals.
  - GitHub deployment guidance states that environments and required reviewers can gate secret access and production deployments.
- Impact:
  - Any actor or automation path that can publish a GitHub release can trigger package publication with the configured NuGet API key without a second source-visible approval boundary.
  - This expands the blast radius of mistaken releases or release-path credential compromise.
  - Exploitability depends partly on hosted repository governance that is not visible locally, but the missing workflow-level gate is confirmed.
- Recommended remediation:
  - Add a dedicated production GitHub environment to the publish job.
  - Move the NuGet publish secret to that environment.
  - Require reviewer approval before the deploy job can access the secret.
  - Consider adding deploy concurrency protection for release publication.
- Recommended validating agent or skill:
  - `csharp-engineering`
  - `security-research`
- Implementation status:
  - `Not implemented by security-researcher`

### SEC-002 `Oracle test container image is pinned only by mutable tag`

- Severity: `Low`
- Confidence: `High`
- Category: `Supply Chain Integrity`
- CWE: `CWE-494 Download of Code Without Integrity Check`
- OWASP: `OWASP Top 10 2021 A08: Software and Data Integrity Failures`
- Affected files or symbols:
  - `tests/integration/Syrx.Oracle.Tests.Integration/OracleFixture.cs`
  - `OracleFixture` container setup
- Evidence:
  - `OracleFixture` defines `OracleImage` as `gvenzl/oracle-xe:21.3.0-slim-faststart`.
  - The container is created from that mutable tag value rather than an immutable digest.
  - Docker guidance identifies digests as the immutable integrity mechanism for image selection, whereas tags can be retargeted.
- Impact:
  - A retagged or compromised upstream image could alter CI execution behavior without any repository source change.
  - The finding is low severity because the image is used in integration tests rather than production deployment, and the associated test job uses restricted repository permissions.
  - Even so, it remains a real software-integrity gap in the trusted build-and-test path.
- Recommended remediation:
  - Pin the Oracle image by digest.
  - Track digest refreshes through a documented maintenance path.
  - Keep the human-readable tag in comments or documentation if needed, but make the runtime reference immutable.
- Recommended validating agent or skill:
  - `csharp-engineering`
  - `security-research`
- Implementation status:
  - `Not implemented by security-researcher`

## Missing skills, information, or tooling

- Missing skill coverage: `None material for this assessment.`
- Missing evidence:
  - GitHub environment settings, required-reviewer rules, and release permission model were not available locally.
  - CodeQL SARIF and Gitleaks output artifacts were not available locally.
  - The terminal tool truncated the tail of the targeted integration-test run, limiting visibility into the final pass/fail summary even though the earlier compile blocker was not reproduced.
- Confidence impact:
  - `SEC-001` remains high confidence because the missing workflow gate is directly visible in source.
  - `SEC-002` remains high confidence because the mutable tag is directly visible in source.
  - Confidence in the statement that the previous `OracleFixture` secret-logging finding is resolved is high because the current source no longer logs `GetConnectionString()`.

## Cross-agent remediation handoff recommendations

| Recommendation | Owner | Why |
|---|---|---|
| Add GitHub environment gating and required approval to package publication | `csharp-engineering` | The current release workflow still exposes the NuGet publish path without a source-visible production approval boundary. |
| Pin the Oracle integration image by digest and document update cadence | `csharp-engineering` | This removes the remaining mutable-tag integrity gap in the CI test path. |
| Optionally capture explicit CI test summaries as artifacts or quieter output | `csharp-engineering` | This would improve future assessment evidence quality, but it is not a current security finding. |

## Appendix

### Files and symbols reviewed

- `Directory.Build.props`
- `global.json`
- `Syrx.Oracle.sln`
- `.github/dependabot.yml`
- `.github/workflows/publish.yml`
- `.github/workflows/security.yml`
- `src/Syrx.Commanders.Databases.Connectors.Oracle/OracleDatabaseConnector.cs`
- `src/Syrx.Commanders.Databases.Oracle/OracleDynamicParameters.cs`
- `tests/integration/Syrx.Oracle.Tests.Integration/OracleFixture.cs`
- `.submodules/Syrx.Commanders.Databases/src/Syrx.Commanders.Databases.Settings.Extensions.Json/UseFileExtensions.cs`
- `.submodules/Syrx.Commanders.Databases/src/Syrx.Commanders.Databases.Settings.Extensions.Xml/UseFileExtensions.cs`

### Searches and diagnostics used

- `git branch --show-current`
- `git status --short --branch`
- `dotnet list Syrx.Oracle.sln package --vulnerable --include-transitive`
- `dotnet test .\tests\integration\Syrx.Oracle.Tests.Integration\Syrx.Oracle.Tests.Integration.csproj --configuration Release`
- Workspace searches for:
  - `GetConnectionString|LogInformation|Console\.WriteLine|WithImage|NUGET_API_KEY|environment:`
  - `UseCommandText\(\$|CommandText\s*=\s*\$|SELECT \* FROM .*\{`
  - `Password=|User Id=|Pwd=|Secret|Token|ApiKey`

### References

- GitHub Docs: `Reviewing deployments`
- GitHub Docs: `Secure use reference`
- Docker Docs: `Image digests`
- MITRE CWE-284: `Improper Access Control`
- MITRE CWE-494: `Download of Code Without Integrity Check`
- Workspace instruction: `.github/instructions/security-and-secure-coding.instructions.md`
- Workspace instruction: `.github/instructions/github-actions-ci-cd-best-practices.instructions.md`

### Assumptions

- The assessment target is the current final working tree on branch `3.0.0`.
- Solution scope includes referenced submodule projects because they participate in the solution build and security posture.
- No hidden repository-level environment protection compensates for the missing `environment:` declaration in source, because such settings are not visible locally.
- The NuGet vulnerability audit result reflects the configured package sources available on the assessment machine.