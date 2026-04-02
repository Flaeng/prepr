# Project Guidelines

## Overview

prepr is a .NET 10.0 CLI tool that detects and reports code quality issues across files in a directory tree — including duplicated code blocks, line-limit violations, excessive nesting depth, and early return opportunities. It uses `System.CommandLine` for CLI parsing and supports multiple output formats (console, HTML, Markdown, CSV, prompt).

## Architecture

- **Program.cs** — Entry point, config merging (CLI > config file > defaults), orchestration
- **CliSymbols.cs** — CLI argument and option definitions
- **ConfigLoader.cs** — Loads and deserializes `.preprrc` JSON config files
- **ConfigInitializer.cs** — Creates a new `.preprrc` config file in a directory
- **Models/** — Immutable domain records (`DuplicateBlock`, `ScanResult`, `DuplicationFileInfo`, `SummaryStatistics`, `TechDebtScore`, etc.)
- **FileDiscovery.cs** — Directory traversal with include/exclude glob filtering
- **DuplicateDetector.cs** — Core duplicate-line detection algorithm
- **BlockConsolidator.cs** — Merges duplicate blocks that appear in the same set of files
- **EarlyReturnAnalyzer.cs** — Detects early return anti-pattern violations
- **RuleChecker.cs** — Orchestrator for duplication, line-limit, nesting-depth, and early-return rules; integrates caching
- **ScanCache.cs** — Incremental scanning cache to skip unchanged files
- **ReportWriter.cs** — Routes scan results to multiple output format reporters
- **ProgressBar.cs** — Terminal progress bar display utility
- **Reporters/** — Output format implementations behind `IReporter` interface

## Code Style

- Use C# records for immutable data types
- Nullable reference types are enabled project-wide
- Implicit usings are enabled
- Normalize file paths with forward slashes for cross-platform consistency

## Terminal Usage

- Use relative paths instead of absolute paths when changing directories
- Run one command at a time; only pipe or chain commands when genuinely necessary

## Build and Test

```bash
dotnet build prepr/prepR.csproj
dotnet test prepr.tests/prepR.Tests.csproj
dotnet run --project prepr -- <path> [options]
```

## Conventions

- **Config hierarchy:** CLI args override `.preprrc` config file, which overrides built-in defaults
- **Config merging:** When a `.preprrc` file exists, it is merged on top of built-in defaults (`PreprConfig.DefaultConfigJson`). Properties **missing** from the file keep their default values. Properties **explicitly set to `null`** disable/clear that rule. This applies to all config properties — scalar values, arrays, and dictionary-based rules alike.
- **Exit codes:** 0 = success, 1 = config/directory error, 2 = thresholds exceeded
- **Severity levels:** Low/Medium/High based on configurable thresholds for each rule
- **Package versions** are centrally managed in `Directory.Packages.props`
- **Shared build properties** live in `Directory.Build.props`
- Tests use xUnit; each core component and reporter has a corresponding test file in `prepr.tests/`
