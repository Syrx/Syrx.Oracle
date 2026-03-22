# Syrx.Oracle Performance Research Report

## Metadata

- Report name: `Syrx.Oracle-performance-research-report-20260322.md`
- Generated on: `2026-03-22`
- Assessor: `performance-researcher`
- Scope: `Syrx.Oracle.sln` on branch `3.0.0`, including root projects, referenced `Syrx.Commanders.Databases` submodule projects, Oracle integration tests, and GitHub workflow shape after the final `OracleFixture` changes
- Report location: `/.docs/research/performance/`
- Evidence sources: source inspection, targeted search, project and workflow inspection, local `dotnet restore`, local `dotnet build --no-restore`, local project-level `dotnet test --no-build --no-restore`, and local solution-level `dotnet test --no-build --no-restore`

## Scope and constraints

- In scope: root solution build configuration, package/build settings, publish workflow shape, Oracle cursor-based multiple-result-set path, inherited Syrx multi-result query path, and Oracle integration test harness behavior after the current fixture changes
- Out of scope: production telemetry, live Oracle execution plans, profiler traces, BenchmarkDotNet microbenchmarks, GitHub-hosted runner timing data, and remediation implementation
- Constraints:
  - No production code changes were made
  - No representative Oracle workload or database-side trace data was available
  - No hosted-runner timing history was available, so CI impact is inferred from workflow shape plus local timings
  - Full-solution test timing is partially obscured because the solution-level no-build test path currently fails during Oracle integration setup with `ORA-00942`

## Methodology and measurement sources

- Workspace instructions reviewed:
  - `.github/skills/performance-research/SKILL.md`
  - `.github/instructions/csharp-development-and-standards.instructions.md`
  - `.github/instructions/async-programming.instructions.md`
  - `.github/instructions/data-access-and-syrx.instructions.md`
  - `.github/instructions/testing-strategy.instructions.md`
  - `.github/instructions/documentation-and-specifications.instructions.md`
  - `.github/instructions/architecture-ddd-and-domain.instructions.md`
  - `.github/instructions/security-and-secure-coding.instructions.md`
  - `.github/instructions/validation-and-guards.instructions.md`
- Skills used: `performance-research`
- Agents consulted through orchestrator: none
- Research methods:
  - Inspected root and submodule solution, project, workflow, and Oracle-specific source files
  - Searched for synchronous blocking, repeated build work, Oracle cursor setup, and multi-result reflection usage
  - Re-ran build and test commands against the current final working tree instead of relying on the earlier report snapshot
- Measurement sources:
  - `dotnet restore .\Syrx.Oracle.sln -v minimal` completed successfully in `35.1s`
  - `dotnet build .\Syrx.Oracle.sln --configuration Release --no-restore` succeeded in `186.6s`
  - `dotnet test` for `Syrx.Commanders.Databases.Connectors.Oracle.Extensions.Tests.Unit` with `--no-build --no-restore` succeeded with `1` test in `6.7s`
  - `dotnet test` for `Syrx.Commanders.Databases.Connectors.Oracle.Tests.Unit` with `--no-build --no-restore` succeeded with `3` tests in `7.5s`
  - `dotnet test` for `Syrx.Oracle.Tests.Integration` with `--no-build --no-restore` succeeded with `106` passed and `6` skipped in `112.9s`, with overall command completion in `116.5s`
  - `dotnet test .\Syrx.Oracle.sln --configuration Release --no-build --no-restore` failed in `65.2s` with `347` total tests, `235` succeeded, `106` failed, and `6` skipped because Oracle integration setup hit `ORA-00942: table or view does not exist`

## Executive summary

The dominant bottleneck remains outside the thin Oracle connector: the repository is paying substantial build and CI pipeline cost from broad package-on-build settings and repeated full-solution work. On the current final working tree, a clean restore took `35.1s` and a no-restore Release build still took `186.6s`, while the publish workflow continues to rebuild and retest the full graph in separate jobs without artifact reuse or package caching.

The Oracle multiple-result-set path still carries avoidable per-call overhead. `OracleDynamicParameters` eagerly allocates sixteen output cursors by default and converts them to a fresh array during binding, while the inherited Syrx multi-result reader reflects into Dapper `GridReader.ReadAsync<T>` for each result set. That is unlikely to dominate large queries, but it is a real throughput and allocation cost on small, frequent cursor-based repository calls.

The earlier fixture-constructor problem is fixed, but the Oracle integration harness still blocks clean performance baselining. Project-level integration tests now run successfully, yet the full solution no-build test path fails with `ORA-00942` during `Installer.SetupDatabase`, which is consistent with a reuse-driven or non-idempotent database setup issue. In addition, the integration suite remains expensive: `112.9s` of test time for `112` integration cases, with container startup and verbose startup logging contributing materially to runtime.

## Findings summary table

| ID | Priority | Confidence | Category | Affected area | Short title |
|---|---|---|---|---|---|
| PERF-001 | High | High | CI/CD / Build | Solution and workflow shape | Package-on-build and repeated full-solution work inflate build and CI wall-clock time |
| PERF-002 | Medium | High | Allocation / Reflection | Oracle multi-result-set path | Oracle cursor setup and reflected result-set dispatch add avoidable per-call overhead |
| PERF-003 | Medium | High | Test Infrastructure / I/O | Oracle integration tests | Reusable-container setup is expensive and currently makes full-solution test runs non-repeatable |

## Detailed findings

### PERF-001 Package-on-build and repeated full-solution work inflate build and CI wall-clock time

- Priority: `High`
- Confidence: `High`
- Category: `CI/CD / Build`
- Affected files or symbols:
  - `Directory.Build.props`
  - `.submodules/Syrx.Commanders.Databases/Directory.Build.props`
  - `.submodules/Syrx.Commanders.Databases/.submodules/Syrx/Directory.Build.props`
  - `.github/workflows/publish.yml`
- Evidence:
  - Root build settings enable `GeneratePackageOnBuild=true` and `EnablePackageValidation=true` for the repository-wide project graph in `Directory.Build.props`
  - The same package-on-build and package-validation settings are duplicated in the nested `Syrx.Commanders.Databases` and `Syrx` submodule `Directory.Build.props` files
  - The publish workflow rebuilds the solution in `create_nuget` with `dotnet build --configuration Release` and later reruns `dotnet test --configuration Release` in a separate job without artifact reuse
  - The publish workflow also repeats `git submodule update --init --recursive` even though checkout already uses `submodules: recursive`
  - Local measurement on the current tree: `dotnet restore` took `35.1s`, and `dotnet build --no-restore` still took `186.6s`
- Impact:
  - Ordinary validation work pays package-generation and package-validation costs even when the job only needs compile or test outputs
  - CI jobs rebuild a large root-plus-submodule graph multiple times, increasing wall-clock duration and runner cost
  - The cost grows with every added project in the nested repository graph, so the current `3.0.0` baseline is likely to age poorly without workflow consolidation
- Recommended remediation:
  - Separate compile, test, and package concerns so package generation and validation are only enabled where release artifacts are actually needed
  - Reuse build artifacts between build and test jobs, or run test with `--no-build` after a validated build artifact is produced
  - Remove redundant submodule initialization steps after checkout has already fetched recursive submodules
  - Add NuGet package caching and measure hosted-runner timings before and after the workflow simplification
- Recommended validating agent or skill:
  - `csharp-engineering`
  - `task-research`
- Validation or benchmarking recommendation:
  - Capture GitHub Actions step timings before and after workflow changes
  - Measure `dotnet test --no-build` versus `dotnet test` on the same runner class
  - Record package counts, artifact sizes, and package-validation time during CI
- Implementation status:
  - `Not implemented by performance-researcher`

### PERF-002 Oracle cursor setup and reflected result-set dispatch add avoidable per-call overhead

- Priority: `Medium`
- Confidence: `High`
- Category: `Allocation / Reflection`
- Affected files or symbols:
  - `src/Syrx.Commanders.Databases.Oracle/OracleDynamicParameters.cs`
  - `.submodules/Syrx.Commanders.Databases/src/Syrx.Commanders.Databases/DatabaseCommander.QueryAsync.Multiple.cs`
- Evidence:
  - `OracleDynamicParameters` creates sixteen cursor names by default with `Enumerable.Range(1, 16).Select(...).ToArray()`
  - `OracleDynamicParameters.AddParameters` passes `oracleParameters.ToArray()` into `OracleCommand.Parameters.AddRange`, allocating a new array on every binding call
  - The inherited `DatabaseCommander.QueryAsync.Multiple` path uses `QueryMultipleAsync`, then reflects into `GridReader.ReadAsync<T>` for each result set via `MakeGenericMethod`, `MethodInfo.Invoke`, a freshly allocated `new object[] { true }`, and reflection over the task `Result` property
- Impact:
  - Small, frequent Oracle cursor-based queries pay setup and reflection cost before row materialization dominates
  - The default sixteen-cursor behavior is disproportionately wasteful for the common one-result-set or two-result-set case
  - Allocation pressure and dispatch overhead will be visible sooner in OLTP-style repository methods and repeated test loops than in long-running analytical calls
- Recommended remediation:
  - Replace reflection-based result-set dispatch with typed cached delegates or another non-reflective dispatch strategy in the shared Syrx layer
  - Stop defaulting to sixteen output cursors when the caller only needs a smaller known count
  - Remove per-call `ToArray()` churn during Oracle parameter binding where the provider API allows stable storage or pre-sized arrays
- Recommended validating agent or skill:
  - `csharp-engineering`
  - `syrx-data-access`
- Validation or benchmarking recommendation:
  - Add focused microbenchmarks for one, two, four, and sixteen-result-set Oracle queries
  - Use `dotnet-counters` or `dotnet-trace` to compare allocation rate and GC activity before and after dispatch changes
  - Benchmark `OracleDynamicParameters.Cursors()` separately from actual query execution to isolate setup overhead
- Implementation status:
  - `Not implemented by performance-researcher`

### PERF-003 Reusable-container setup is expensive and currently makes full-solution test runs non-repeatable

- Priority: `Medium`
- Confidence: `High`
- Category: `Test Infrastructure / I/O`
- Affected files or symbols:
  - `tests/integration/Syrx.Oracle.Tests.Integration/OracleFixture.cs`
  - `tests/integration/Syrx.Oracle.Tests.Integration/Installer.cs`
  - `tests/integration/Syrx.Oracle.Tests.Integration/DatabaseBuilder.cs`
- Evidence:
  - `OracleFixture` enables `WithReuse(true)` and then calls `Installer.SetupDatabase` during `InitializeAsync` on every fixture initialization
  - `Installer.SetupDatabase` constructs `DatabaseBuilder` and always executes `builder.Build()`
  - `DatabaseBuilder.Build()` performs repeated DDL and data population work including `CreatePocoTable`, `CreateWritesTable`, procedure drops and creates, `ClearTable`, and `Populate`
  - The fixture also emits a large multi-line startup log payload for every startup callback and offloads a trivial console write via `Task.Run` in `DisposeAsync`
  - Project-level integration tests succeeded in `112.9s` for `106` passed and `6` skipped tests, showing the fixture constructor issue is resolved but startup/runtime cost remains high
  - Full-solution `dotnet test --no-build --no-restore` failed in `65.2s` with `106` Oracle integration failures and `ORA-00942: table or view does not exist` during `DatabaseBuilder.Populate` and `Installer.SetupDatabase`, which indicates the current reuse plus setup path is not repeatable at solution scope
- Impact:
  - The Oracle integration suite is expensive even when it passes, consuming nearly two minutes for a single integration project on the measured machine
  - Reuse without clearly idempotent schema setup invalidates solution-level baselines because the test path can fail before meaningful throughput comparisons are made
  - Verbose startup logging and unnecessary thread-pool work are small individually, but they add avoidable overhead to the slowest validation path in the repository
- Recommended remediation:
  - Decide whether the integration harness should prioritize clean ephemeral databases or warm reusable containers, then make the schema setup explicitly idempotent for that model
  - Move expensive one-time schema creation out of per-fixture setup where possible, or make setup detect and reset state deterministically
  - Remove unnecessary `Task.Run` usage in disposal and trim startup logging to the fields needed for diagnosis
  - Isolate integration tests into a dedicated workflow/job so their container lifecycle and timing can be measured independently from fast unit validation
- Recommended validating agent or skill:
  - `debug`
  - `csharp-engineering`
- Validation or benchmarking recommendation:
  - Measure cold-start versus warm-start suite timings separately
  - Record fixture startup duration, schema setup duration, and container reuse hit rate in CI logs
  - Re-run full solution `dotnet test --no-build --no-restore` after making setup deterministic and compare pass rate plus wall-clock time
- Implementation status:
  - `Not implemented by performance-researcher`

## Missing skills, information, instrumentation, or tooling

- Missing skill coverage: none identified for this assessment
- Missing evidence:
  - No production latency, throughput, allocation, or GC counters
  - No Oracle execution plans, wait-event data, or server-side traces
  - No BenchmarkDotNet coverage for the Oracle cursor or shared multi-result dispatch path
  - No GitHub-hosted runner timing history for the publish workflow
- Confidence impact:
  - Confidence is high for workflow-shape and source-level allocation findings because the evidence is static and direct
  - Confidence is medium for exact runtime magnitude of the Oracle data-access hot path because no profiler or targeted benchmark was available
  - Confidence is high that integration performance baselines are currently distorted because the solution-level test path now fails reproducibly with `ORA-00942` during Oracle setup

## Cross-agent remediation handoff recommendations

| Recommendation | Owner | Why |
|---|---|---|
| Simplify root and submodule build and publish workflows to avoid repeated full-solution build, test, and package work | `csharp-engineering` | The most expensive measurable cost is workflow and build shape rather than connector logic |
| Refactor the shared multi-result-set path to avoid reflection-heavy dispatch and excessive cursor setup allocation | `csharp-engineering` with `syrx-data-access` | The hot path spans Oracle-specific parameter handling and shared Syrx query infrastructure |
| Reproduce and stabilize the reusable-container `ORA-00942` failure before trusting solution-level performance baselines | `debug` with `csharp-engineering` | The current integration harness is functionally unstable at solution scope and distorts timing data |
| Consider an ADR if the team changes package-on-build policy or integration test execution strategy across root and submodule repositories | `adr-generator` | Those choices affect multiple repositories and release workflows, not just a single project |

## Appendix: searched files, commands, traces, references, and assumptions

### Files and symbols reviewed

- `Syrx.Oracle.sln`
- `Directory.Build.props`
- `global.json`
- `.github/workflows/publish.yml`
- `src/Syrx.Commanders.Databases.Connectors.Oracle/OracleDatabaseConnector.cs`
- `src/Syrx.Commanders.Databases.Oracle/OracleDynamicParameters.cs`
- `tests/integration/Syrx.Oracle.Tests.Integration/OracleFixture.cs`
- `tests/integration/Syrx.Oracle.Tests.Integration/Installer.cs`
- `tests/integration/Syrx.Oracle.Tests.Integration/DatabaseBuilder.cs`
- `tests/integration/Syrx.Oracle.Tests.Integration/Syrx.Oracle.Tests.Integration.csproj`
- `.submodules/Syrx.Commanders.Databases/src/Syrx.Commanders.Databases/DatabaseCommander.QueryAsync.Multiple.cs`
- `.submodules/Syrx.Commanders.Databases/Directory.Build.props`
- `.submodules/Syrx.Commanders.Databases/.submodules/Syrx/Directory.Build.props`

### Searches and diagnostics used

- Searched for `GeneratePackageOnBuild`, `EnablePackageValidation`, and recursive submodule update steps
- Searched for `.Wait(`, `.Result`, `Task.Run(`, `QueryMultipleAsync`, `MakeGenericMethod`, `Enumerable.Range`, and `ToArray()`
- Ran `dotnet --info`
- Ran `dotnet restore .\Syrx.Oracle.sln -v minimal`
- Ran `dotnet build .\Syrx.Oracle.sln --configuration Release --no-restore`
- Ran project-level unit and integration tests with `--no-build --no-restore`
- Ran `dotnet test .\Syrx.Oracle.sln --configuration Release --no-build --no-restore`

### References

- Workspace performance-research skill template and constraints
- Workspace async, C#, testing, validation, architecture, and data-access instructions listed in the Methodology section

### Assumptions

- The current working tree represents the intended final `3.0.0` post-fixture-change state on branch `3.0.0`
- Local timings are directional and should not be treated as substitutes for hosted-runner or production timings
- Oracle cursor-based multiple result sets are a meaningful supported scenario because the package explicitly provides Oracle cursor support for that purpose